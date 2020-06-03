﻿/*

  Copyright (C) 2020 Kunio Fukuchi

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Kunio Fukuchi

*/

namespace OpenPOS.CCO115Interop
{
    using Microsoft.PointOfService;
    using OpenPOS.Devices;
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    [ServiceObject(DeviceType.Biometrics, "OpenPOS 1.15 Biometrics", "OPOS Biometrics CCO Interop", 1, 15)]
    public class OpenPOSBiometrics : Biometrics, ILegacyControlObject, IDisposable
    {
        private OpenPOS.Devices.OPOSBiometrics _cco = null;
        private const string _oposDeviceClass = "Biometrics";
        private string _oposDeviceName = "";
        private int _binaryConversion = 0;

        #region Event handler management variable

        public override event DataEventHandler DataEvent;

        public override event DirectIOEventHandler DirectIOEvent;

        public override event DeviceErrorEventHandler ErrorEvent;

        public override event StatusUpdateEventHandler StatusUpdateEvent;

        #endregion Event handler management variable

        #region Constructor, Destructor

        public OpenPOSBiometrics()
        {
            _cco = null;
            _oposDeviceName = "";
            _binaryConversion = 0;
        }

        ~OpenPOSBiometrics()
        {
            Dispose(false);
        }

        #region IDisposable Support

        private bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: Discard the managed state (managed object).
                }

                if (_cco != null)
                {
                    _cco.DataEvent -= (_IOPOSBiometricsEvents_DataEventEventHandler)_cco_DataEvent;
                    _cco.DirectIOEvent -= (_IOPOSBiometricsEvents_DirectIOEventEventHandler)_cco_DirectIOEvent;
                    _cco.ErrorEvent -= (_IOPOSBiometricsEvents_ErrorEventEventHandler)_cco_ErrorEvent;
                    _cco.StatusUpdateEvent -= (_IOPOSBiometricsEvents_StatusUpdateEventEventHandler)_cco_StatusUpdateEvent;
                    _cco = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support

        #endregion Constructor, Destructor

        #region Utility subroutine

        /// <summary>
        /// Check the processing result value of OPOS and generate a PosControlException exception if it is an error.
        /// </summary>
        /// <param name="value">OPOS method return value or ResultCode property value</param>
        private void VerifyResult(int value)
        {
            if (value != (int)ErrorCode.Success)
            {
                ErrorCode eValue = (ErrorCode)InteropEnum<ErrorCode>.ToEnumFromInteger(value);
                throw new Microsoft.PointOfService.PosControlException((_oposDeviceClass + ":" + _oposDeviceName), eValue, _cco.ResultCodeExtended);
            }
        }

        private BiometricsInformationRecord ToBiometricsInformationRecordFromString(string value)
        {
            BiometricsInformationRecord Result = null;

            if ((!string.IsNullOrEmpty(value)) && (value.Length >= 45))
            {
                byte[] array = InteropCommon.ToByteArrayFromString(value, _binaryConversion);
                int BDBLength = BitConverter.ToInt32(array, 0) - 45;
                Version headerVersion = new Version((int)array[4], 0);
                BirDataTypes bdt = (BirDataTypes)InteropEnum<BirDataTypes>.ToEnumFromInteger((int)array[5]);
                int formatIDOwner = BitConverter.ToUInt16(array, 7);
                int formatIDType = BitConverter.ToUInt16(array, 9);
                BirPurpose BIRPurpose = (BirPurpose)InteropEnum<BirPurpose>.ToEnumFromInteger((int)array[11]);
                int biometricTypeInteger = BitConverter.ToInt32(array, 12);
                SensorType biometricType = (SensorType)InteropEnum<SensorType>.ToEnumFromInteger(biometricTypeInteger);
                byte[] bdb = null;
                if (BDBLength > 0)
                {
                    bdb = new byte[BDBLength];
                    Array.Copy(array, 45, bdb, 0, BDBLength);
                }
                Result = new BiometricsInformationRecord(headerVersion, bdt, formatIDOwner, formatIDType, BIRPurpose, biometricType, bdb);
            }

            return Result;
        }

        private byte[] ToByteArrayFromBiometricsInformationRecord(BiometricsInformationRecord value)
        {
            if (value == null) return null;

            byte[] Result = null;

            try
            {
                int length = value.BiometricDataBlockSize + 45;
                Result = new byte[length];
                Array.Clear(Result, 0, length);
                byte[] work = BitConverter.GetBytes(length);
                Array.Copy(work, Result, 4);
                Result[4] = (byte)value.Version.Major;
                Result[5] = (byte)(int)value.DataType;
                work = BitConverter.GetBytes(value.FormatOwner);
                Result[6] = work[0];
                Result[7] = work[1];
                work = BitConverter.GetBytes(value.FormatId);
                Result[8] = work[0];
                Result[9] = work[1];
                Result[11] = (byte)(int)value.Purpose;
                work = BitConverter.GetBytes((int)value.SensorType);
                Array.Copy(work, 0, Result, 12, 4);
                if (value.BiometricDataBlockSize > 0)
                {
                    Array.Copy(value.GetBiometricDataBlock(), 0, Result, 45, value.BiometricDataBlockSize);
                }
            }
            catch
            {
                Result = null;
            }

            return Result;
        }

        private string[] ToStringArrayFromBiometricsInformationRecords(IEnumerable<BiometricsInformationRecord> birPopulation)
        {
            if (birPopulation == null) return null;

            string[] Result = null;

            try
            {
                List<string> BIRStringList = new List<string>();

                foreach (BiometricsInformationRecord BIR in birPopulation)
                {
                    string BIRString = InteropCommon.ToStringFromByteArray(ToByteArrayFromBiometricsInformationRecord(BIR), _binaryConversion);

                    if (!string.IsNullOrEmpty(BIRString))
                    {
                        BIRStringList.Add(BIRString);
                    }
                }

                if (BIRStringList.Count > 0)
                {
                    Result = BIRStringList.ToArray();
                }
            }
            catch
            {
                ;
            }

            return Result;
        }

        #endregion Utility subroutine

        #region Process of relaying OPOS event and generating POS for.NET event

        private void _cco_DataEvent(int Status)
        {
            if (this.DataEvent != null)
            {
                DataEvent(this, new DataEventArgs(Status));
            }
        }

        private void _cco_DirectIOEvent(int EventNumber, ref int pData, ref string pString)
        {
            if (this.DirectIOEvent != null)
            {
                DirectIOEventArgs eDE = new DirectIOEventArgs(EventNumber, pData, pString);
                DirectIOEvent(this, eDE);
                pData = eDE.Data;
                pString = Convert.ToString(eDE.Object);
            }
        }

        private void _cco_ErrorEvent(int ResultCode, int ResultCodeExtended, int ErrorLocus, ref int pErrorResponse)
        {
            if (this.ErrorEvent != null)
            {
                ErrorCode eCode = (ErrorCode)InteropEnum<ErrorCode>.ToEnumFromInteger(ResultCode);
                ErrorLocus eLocus = (ErrorLocus)InteropEnum<ErrorLocus>.ToEnumFromInteger(ErrorLocus);
                ErrorResponse eResponse = (ErrorResponse)InteropEnum<ErrorResponse>.ToEnumFromInteger(pErrorResponse);
                DeviceErrorEventArgs eEE = new DeviceErrorEventArgs(eCode, ResultCodeExtended, eLocus, eResponse);
                ErrorEvent(this, eEE);
                pErrorResponse = (int)eEE.ErrorResponse;
            }
        }

        private void _cco_StatusUpdateEvent(int Data)
        {
            if (this.StatusUpdateEvent != null)
            {
                StatusUpdateEvent(this, new StatusUpdateEventArgs(Data));
            }
        }

        #endregion Process of relaying OPOS event and generating POS for.NET event

        #region ILegacyControlObject member

        public BinaryConversion BinaryConversion
        {
            get
            {
                return (BinaryConversion)InteropEnum<BinaryConversion>.ToEnumFromInteger(_cco.BinaryConversion);
            }
            set
            {
                _cco.BinaryConversion = (int)value;
                VerifyResult(_cco.ResultCode);
                _binaryConversion = _cco.BinaryConversion;
            }
        }

        public string ControlObjectDescription
        {
            get { return _cco.ControlObjectDescription; }
        }

        public Version ControlObjectVersion
        {
            get { return InteropCommon.ToVersion(_cco.ControlObjectVersion); }
        }

        #endregion ILegacyControlObject member

        #region Device common properties

        public override bool CapCompareFirmwareVersion
        {
            get { return _cco.CapCompareFirmwareVersion; }
        }

        public override PowerReporting CapPowerReporting
        {
            get { return (PowerReporting)InteropEnum<PowerReporting>.ToEnumFromInteger(_cco.CapPowerReporting); }
        }

        public override bool CapStatisticsReporting
        {
            get { return _cco.CapStatisticsReporting; }
        }

        public override bool CapUpdateFirmware
        {
            get { return _cco.CapUpdateFirmware; }
        }

        public override bool CapUpdateStatistics
        {
            get { return _cco.CapUpdateStatistics; }
        }

        public override bool AutoDisable
        {
            get
            {
                return _cco.AutoDisable;
            }
            set
            {
                _cco.AutoDisable = value;
                VerifyResult(_cco.ResultCode);
            }
        }

        public override string CheckHealthText
        {
            get { return _cco.CheckHealthText; }
        }

        public override bool Claimed
        {
            get { return _cco.Claimed; }
        }

        public override int DataCount
        {
            get { return _cco.DataCount; }
        }

        public override bool DataEventEnabled
        {
            get
            {
                return _cco.DataEventEnabled;
            }
            set
            {
                _cco.DataEventEnabled = value;
                VerifyResult(_cco.ResultCode);
            }
        }

        public override bool DeviceEnabled
        {
            get
            {
                return _cco.DeviceEnabled;
            }
            set
            {
                _cco.DeviceEnabled = value;
                VerifyResult(_cco.ResultCode);
            }
        }

        public override bool FreezeEvents
        {
            get
            {
                return _cco.FreezeEvents;
            }
            set
            {
                _cco.FreezeEvents = value;
                VerifyResult(_cco.ResultCode);
            }
        }

        public override PowerNotification PowerNotify
        {
            get
            {
                return (PowerNotification)InteropEnum<PowerNotification>.ToEnumFromInteger(_cco.PowerNotify);
            }
            set
            {
                _cco.PowerNotify = (int)value;
                VerifyResult(_cco.ResultCode);
            }
        }

        public override PowerState PowerState
        {
            get { return (PowerState)InteropEnum<PowerState>.ToEnumFromInteger(_cco.PowerState); }
        }

        public override ControlState State
        {
            get { return (ControlState)InteropEnum<ControlState>.ToEnumFromInteger(_cco.State); }
        }

        public override string ServiceObjectDescription
        {
            get { return _cco.ServiceObjectDescription; }
        }

        public override Version ServiceObjectVersion
        {
            get { return InteropCommon.ToVersion(_cco.ControlObjectVersion); }
        }

        public override string DeviceDescription
        {
            get { return _cco.DeviceDescription; }
        }

        public override string DeviceName
        {
            get { return _cco.DeviceName; }
        }

        #endregion Device common properties

        #region Device common method

        public override void Open()
        {
            if (string.IsNullOrWhiteSpace(_oposDeviceName))
            {
                try
                {
                    _oposDeviceName = GetConfigurationProperty("OposDeviceName");
                    _oposDeviceName.Trim();
                }
                catch
                {
                    _oposDeviceName = "";
                }
            }

            if (string.IsNullOrWhiteSpace(_oposDeviceName))
            {
                string strMessage = "OposDeviceName is not configured on " + DevicePath + ".";
                throw new Microsoft.PointOfService.PosControlException(strMessage, ErrorCode.NoExist);
            }

            if (_cco == null)
            {
                try
                {
                    // CCO object CreateInstance
                    _cco = new OpenPOS.Devices.OPOSBiometrics();

                    // Register event handler
                    _cco.DataEvent += new _IOPOSBiometricsEvents_DataEventEventHandler(_cco_DataEvent);
                    _cco.DirectIOEvent += new _IOPOSBiometricsEvents_DirectIOEventEventHandler(_cco_DirectIOEvent);
                    _cco.ErrorEvent += new _IOPOSBiometricsEvents_ErrorEventEventHandler(_cco_ErrorEvent);
                    _cco.StatusUpdateEvent += new _IOPOSBiometricsEvents_StatusUpdateEventEventHandler(_cco_StatusUpdateEvent);
                }
                catch
                {
                    string strMessage = "Can not create Common ControlObject on " + DevicePath + ".";
                    throw new Microsoft.PointOfService.PosControlException(strMessage, ErrorCode.Failure);
                }
            }

            VerifyResult(_cco.Open(_oposDeviceName));
        }

        public override void Close()
        {
            VerifyResult(_cco.Close());

            _cco.DataEvent -= (_IOPOSBiometricsEvents_DataEventEventHandler)_cco_DataEvent;
            _cco.DirectIOEvent -= (_IOPOSBiometricsEvents_DirectIOEventEventHandler)_cco_DirectIOEvent;
            _cco.ErrorEvent -= (_IOPOSBiometricsEvents_ErrorEventEventHandler)_cco_ErrorEvent;
            _cco.StatusUpdateEvent -= (_IOPOSBiometricsEvents_StatusUpdateEventEventHandler)_cco_StatusUpdateEvent;
            _cco = null;
        }

        public override void Claim(int timeout)
        {
            VerifyResult(_cco.ClaimDevice(timeout));
        }

        public override void Release()
        {
            VerifyResult(_cco.ReleaseDevice());
        }

        public override string CheckHealth(HealthCheckLevel level)
        {
            VerifyResult(_cco.CheckHealth((int)level));
            return _cco.CheckHealthText;
        }

        public override void ClearInput()
        {
            VerifyResult(_cco.ClearInput());
        }

        public override void ClearInputProperties()
        {
            VerifyResult(_cco.ClearInputProperties());
        }

        public override DirectIOData DirectIO(int command, int data, object obj)
        {
            var intValue = data;
            var stringValue = Convert.ToString(obj);
            VerifyResult(_cco.DirectIO(command, ref intValue, ref stringValue));
            return new DirectIOData(intValue, stringValue);
        }

        public override CompareFirmwareResult CompareFirmwareVersion(string firmwareFileName)
        {
            int result;
            VerifyResult(_cco.CompareFirmwareVersion(firmwareFileName, out result));
            return (CompareFirmwareResult)InteropEnum<CompareFirmwareResult>.ToEnumFromInteger(result);
        }

        public override void UpdateFirmware(string firmwareFileName)
        {
            VerifyResult(_cco.UpdateFirmware(firmwareFileName));
        }

        public override void ResetStatistic(string statistic)
        {
            VerifyResult(_cco.ResetStatistics(statistic));
        }

        public override void ResetStatistics(string[] statistics)
        {
            VerifyResult(_cco.ResetStatistics(string.Join(",", statistics)));
        }

        public override void ResetStatistics(StatisticCategories statistics)
        {
            VerifyResult(_cco.ResetStatistics(Enum.GetName(typeof(StatisticCategories), statistics)));
        }

        public override void ResetStatistics()
        {
            VerifyResult(_cco.ResetStatistics(""));
        }

        public override string RetrieveStatistic(string statistic)
        {
            var result = statistic;
            VerifyResult(_cco.RetrieveStatistics(ref result));
            return result;
        }

        public override string RetrieveStatistics(string[] statistics)
        {
            var result = string.Join(",", statistics);
            VerifyResult(_cco.RetrieveStatistics(ref result));
            return result;
        }

        public override string RetrieveStatistics(StatisticCategories statistics)
        {
            var result = Enum.GetName(typeof(StatisticCategories), statistics);
            VerifyResult(_cco.RetrieveStatistics(ref result));
            return result;
        }

        public override string RetrieveStatistics()
        {
            var result = "";
            VerifyResult(_cco.RetrieveStatistics(ref result));
            return result;
        }

        public override void UpdateStatistic(string name, object value)
        {
            VerifyResult(_cco.UpdateStatistics(name + "=" + value));
        }

        public override void UpdateStatistics(StatisticCategories statistics, object value)
        {
            VerifyResult(_cco.UpdateStatistics(Enum.GetName(typeof(StatisticCategories), statistics) + "=" + value));
        }

        public override void UpdateStatistics(Statistic[] statistics)
        {
            VerifyResult(_cco.UpdateStatistics(InteropCommon.ToStatisticsString(statistics)));
        }

        #endregion Device common method

        #region OPOSBiometrics  Specific Properties

        public override bool CapPrematchData
        {
            get { return _cco.CapPrematchData; }
        }

        public override bool CapRawSensorData
        {
            get { return _cco.CapRawSensorData; }
        }

        public override bool CapRealTimeData
        {
            get { return _cco.CapRealTimeData; }
        }

        public override CapSensorColors CapSensorColor
        {
            get { return (CapSensorColors)InteropEnum<CapSensorColors>.ToEnumFromInteger(_cco.CapSensorColor); }
        }

        public override CapSensorOrientations CapSensorOrientation
        {
            get { return (CapSensorOrientations)InteropEnum<CapSensorOrientations>.ToEnumFromInteger(_cco.CapSensorOrientation); }
        }

        public override CapSensorTypes CapSensorType
        {
            get { return (CapSensorTypes)InteropEnum<CapSensorTypes>.ToEnumFromInteger(_cco.CapSensorType); }
        }

        public override bool CapTemplateAdaptation
        {
            get { return _cco.CapTemplateAdaptation; }
        }

        public override int Algorithm
        {
            get
            {
                return _cco.Algorithm;
            }
            set
            {
                _cco.Algorithm = value;
                VerifyResult(_cco.ResultCode);
            }
        }

        public override string[] AlgorithmList
        {
            get
            {
                List<string> Result = new List<string>();
                string ListCSVString = _cco.AlgorithmList;

                if (!string.IsNullOrWhiteSpace(ListCSVString))
                {
                    foreach (string s in ListCSVString.Split(','))
                    {
                        if (string.IsNullOrWhiteSpace(s))
                        {
                            continue;
                        }

                        Result.Add(s.Trim());
                    }
                }

                return Result.ToArray();
            }
        }

        public override BiometricsInformationRecord BiometricsInformationRecord
        {
            get
            {
                BiometricsInformationRecord Result = null;
                string BIRString = _cco.BIR;

                if (!string.IsNullOrEmpty(BIRString))
                {
                    Result = ToBiometricsInformationRecordFromString(BIRString);
                }

                return Result;
            }
        }

        public override System.Drawing.Bitmap RawSensorData
        {
            get
            {
                Bitmap Result = null;
                string RSDString = _cco.RawSensorData;

                if (!string.IsNullOrEmpty(RSDString))
                {
                    int Width = _cco.SensorWidth;
                    int Height = _cco.SensorHeight;
                    int Color = _cco.SensorColor;
                    int BPP = _cco.SensorBPP;
                    int Stride = (Width * BPP);
                    Stride = (((Stride % 4) == 0 ? 0 : 1) + (Stride / 4)) * 4;
                    System.Drawing.Imaging.PixelFormat PF = System.Drawing.Imaging.PixelFormat.Undefined;

                    switch (Color)
                    {
                        case 1: PF = System.Drawing.Imaging.PixelFormat.Format1bppIndexed; break;

                        case 2: PF = System.Drawing.Imaging.PixelFormat.Format16bppGrayScale; break;

                        case 4: PF = System.Drawing.Imaging.PixelFormat.Format4bppIndexed; break;

                        case 8: PF = System.Drawing.Imaging.PixelFormat.Format8bppIndexed; break;

                        case 16:
                            switch (BPP)
                            {
                                case 16: PF = System.Drawing.Imaging.PixelFormat.Format16bppRgb555; break;

                                case 24: PF = System.Drawing.Imaging.PixelFormat.Format24bppRgb; break;

                                case 32: PF = System.Drawing.Imaging.PixelFormat.Format32bppRgb; break;

                                case 48: PF = System.Drawing.Imaging.PixelFormat.Format48bppRgb; break;

                                case 64: PF = System.Drawing.Imaging.PixelFormat.Format64bppArgb; break;
                            }

                            break;
                    }

                    byte[] array = InteropCommon.ToByteArrayFromString(RSDString, _binaryConversion);
                    Result = new Bitmap(Width, Height, Stride, PF, (IntPtr)array[0]);
                }

                return Result;
            }
        }

        public override bool RealTimeDataEnabled
        {
            get
            {
                return _cco.RealTimeDataEnabled;
            }
            set
            {
                _cco.RealTimeDataEnabled = value;
                VerifyResult(_cco.ResultCode);
            }
        }

        public override SensorColor SensorColor
        {
            get
            {
                return (SensorColor)InteropEnum<SensorColor>.ToEnumFromInteger(_cco.SensorColor);
            }
            set
            {
                _cco.SensorColor = (int)value;
                VerifyResult(_cco.ResultCode);
            }
        }

        public override SensorOrientation SensorOrientation
        {
            get
            {
                return (SensorOrientation)InteropEnum<SensorOrientation>.ToEnumFromInteger(_cco.SensorOrientation);
            }
            set
            {
                _cco.SensorColor = (int)value;
                VerifyResult(_cco.ResultCode);
            }
        }

        public override SensorType SensorType
        {
            get
            {
                return (SensorType)InteropEnum<SensorType>.ToEnumFromInteger(_cco.SensorType);
            }
            set
            {
                _cco.SensorColor = (int)value;
                VerifyResult(_cco.ResultCode);
            }
        }

        #endregion OPOSBiometrics  Specific Properties

        #region OPOSBiometrics  Specific Methodss

        public override void BeginEnrollCapture(BiometricsInformationRecord referenceBir, byte[] payload)
        {
            VerifyResult(_cco.BeginEnrollCapture(InteropCommon.ToStringFromByteArray(ToByteArrayFromBiometricsInformationRecord(referenceBir), _binaryConversion), InteropCommon.ToStringFromByteArray(payload, _binaryConversion)));
        }

        public override void BeginVerifyCapture()
        {
            VerifyResult(_cco.BeginVerifyCapture());
        }

        public override void EndCapture()
        {
            VerifyResult(_cco.EndCapture());
        }

        public override int[] Identify(int maximumFalseAcceptRateRequested, int maximumFalseRejectRateRequested, bool falseAcceptRatePrecedence, IEnumerable<BiometricsInformationRecord> referenceBirPopulation, int timeout)
        {
            string[] ReferenceBIRPopulation = ToStringArrayFromBiometricsInformationRecords(referenceBirPopulation);
            int[] CandidateRanking = new int[1] { 0 };
            VerifyResult(_cco.Identify(maximumFalseAcceptRateRequested, maximumFalseRejectRateRequested, falseAcceptRatePrecedence, ReferenceBIRPopulation, CandidateRanking, timeout));
            return CandidateRanking;
        }

        public override int[] IdentifyMatch(int maximumFalseAcceptRateRequested, int maximumFalseRejectRateRequested, bool falseAcceptRatePrecedence, BiometricsInformationRecord sampleBir, IEnumerable<BiometricsInformationRecord> referenceBirPopulation)
        {
            string[] ReferenceBIRPopulation = ToStringArrayFromBiometricsInformationRecords(referenceBirPopulation);
            int[] CandidateRanking = new int[1] { 0 };
            VerifyResult(_cco.IdentifyMatch(maximumFalseAcceptRateRequested, maximumFalseRejectRateRequested, falseAcceptRatePrecedence, InteropCommon.ToStringFromByteArray(ToByteArrayFromBiometricsInformationRecord(sampleBir), _binaryConversion), ReferenceBIRPopulation, CandidateRanking));
            return CandidateRanking;
        }

        public override BiometricsInformationRecord ProcessPrematchData(BiometricsInformationRecord sampleBir, BiometricsInformationRecord prematchDataBir)
        {
            string ProcessedBIR = "";
            VerifyResult(_cco.ProcessPrematchData(InteropCommon.ToStringFromByteArray(ToByteArrayFromBiometricsInformationRecord(sampleBir), _binaryConversion), InteropCommon.ToStringFromByteArray(ToByteArrayFromBiometricsInformationRecord(prematchDataBir), _binaryConversion), ProcessedBIR));
            return ToBiometricsInformationRecordFromString(ProcessedBIR);
        }

        public override BiometricsVerifyResult Verify(int maximumFalseAcceptRateRequested, int maximumFalseRejectRateRequested, bool falseAcceptRatePrecedence, BiometricsInformationRecord referenceBir, bool adaptBir, int timeout)
        {
            string AdaptedBIRString = adaptBir ? "" : null;
            int[] CandidateRanking = new int[1] { 0 };
            bool Result = false;
            int FARArchived = 0;
            int FRRArchived = 0;
            string Payload = "";
            VerifyResult(_cco.Verify(maximumFalseAcceptRateRequested, maximumFalseRejectRateRequested, falseAcceptRatePrecedence, InteropCommon.ToStringFromByteArray(ToByteArrayFromBiometricsInformationRecord(referenceBir), _binaryConversion), AdaptedBIRString, Result, FARArchived, FRRArchived, Payload, timeout));
            BiometricsInformationRecord AdaptedBIR = null;

            if (adaptBir && !string.IsNullOrEmpty(AdaptedBIRString))
            {
                AdaptedBIR = ToBiometricsInformationRecordFromString(AdaptedBIRString);
            }

            return new BiometricsVerifyResult(Result, FARArchived, FRRArchived, AdaptedBIR, InteropCommon.ToByteArrayFromString(Payload, _binaryConversion));
        }

        public override BiometricsVerifyResult VerifyMatch(int maximumFalseAcceptRateRequested, int maximumFalseRejectRateRequested, bool falseAcceptRatePrecedence, BiometricsInformationRecord sampleBir, BiometricsInformationRecord referenceBir, bool adaptBir)
        {
            string AdaptedBIRString = adaptBir ? "" : null;
            int[] CandidateRanking = new int[1] { 0 };
            bool Result = false;
            int FARArchived = 0;
            int FRRArchived = 0;
            string Payload = "";
            VerifyResult(_cco.VerifyMatch(maximumFalseAcceptRateRequested, maximumFalseRejectRateRequested, falseAcceptRatePrecedence, InteropCommon.ToStringFromByteArray(ToByteArrayFromBiometricsInformationRecord(sampleBir), _binaryConversion), InteropCommon.ToStringFromByteArray(ToByteArrayFromBiometricsInformationRecord(referenceBir), _binaryConversion), AdaptedBIRString, Result, FARArchived, FRRArchived, Payload));
            BiometricsInformationRecord AdaptedBIR = null;

            if (adaptBir && !string.IsNullOrEmpty(AdaptedBIRString))
            {
                AdaptedBIR = ToBiometricsInformationRecordFromString(AdaptedBIRString);
            }

            return new BiometricsVerifyResult(Result, FARArchived, FRRArchived, AdaptedBIR, InteropCommon.ToByteArrayFromString(Payload, _binaryConversion));
        }

        #endregion OPOSBiometrics  Specific Methodss
    }
}