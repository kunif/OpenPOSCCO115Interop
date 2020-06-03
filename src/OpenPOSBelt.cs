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

    [ServiceObject(DeviceType.Belt, "OpenPOS 1.15 Belt", "OPOS Belt CCO Interop", 1, 15)]
    public class OpenPOSBelt : Belt, ILegacyControlObject, IDisposable
    {
        private OpenPOS.Devices.OPOSBelt _cco = null;
        private const string _oposDeviceClass = "Belt";
        private string _oposDeviceName = "";
        private int _binaryConversion = 0;

        #region Event handler management variable

        public override event DirectIOEventHandler DirectIOEvent;

        public override event StatusUpdateEventHandler StatusUpdateEvent;

        #endregion Event handler management variable

        #region Constructor, Destructor

        public OpenPOSBelt()
        {
            _cco = null;
            _oposDeviceName = "";
            _binaryConversion = 0;
        }

        ~OpenPOSBelt()
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
                    _cco.DirectIOEvent -= (OpenPOS.Devices._IOPOSBeltEvents_DirectIOEventEventHandler)_cco_DirectIOEvent;
                    _cco.StatusUpdateEvent -= (OpenPOS.Devices._IOPOSBeltEvents_StatusUpdateEventEventHandler)_cco_StatusUpdateEvent;
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

        #endregion Utility subroutine

        #region Process of relaying OPOS event and generating POS for.NET event

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

        public override string CheckHealthText
        {
            get { return _cco.CheckHealthText; }
        }

        public override bool Claimed
        {
            get { return _cco.Claimed; }
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
                    _cco = new OpenPOS.Devices.OPOSBelt();

                    // Register event handler
                    _cco.DirectIOEvent += new _IOPOSBeltEvents_DirectIOEventEventHandler(_cco_DirectIOEvent);
                    _cco.StatusUpdateEvent += new _IOPOSBeltEvents_StatusUpdateEventEventHandler(_cco_StatusUpdateEvent);
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

            _cco.DirectIOEvent -= (OpenPOS.Devices._IOPOSBeltEvents_DirectIOEventEventHandler)_cco_DirectIOEvent;
            _cco.StatusUpdateEvent -= (OpenPOS.Devices._IOPOSBeltEvents_StatusUpdateEventEventHandler)_cco_StatusUpdateEvent;
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

        #region OPOSBelt Specific Properties

        public override bool CapAutoStopBackward
        {
            get { return _cco.CapAutoStopBackward; }
        }

        public override bool CapAutoStopBackwardItemCount
        {
            get { return _cco.CapAutoStopBackwardItemCount; }
        }

        public override bool CapAutoStopForward
        {
            get { return _cco.CapAutoStopForward; }
        }

        public override bool CapAutoStopForwardItemCount
        {
            get { return _cco.CapAutoStopForwardItemCount; }
        }

        public override bool CapLightBarrierBackward
        {
            get { return _cco.CapLightBarrierBackward; }
        }

        public override bool CapLightBarrierForward
        {
            get { return _cco.CapLightBarrierForward; }
        }

        public override bool CapMoveBackward
        {
            get { return _cco.CapMoveBackward; }
        }

        public override bool CapSecurityFlapBackward
        {
            get { return _cco.CapSecurityFlapBackward; }
        }

        public override bool CapSecurityFlapForward
        {
            get { return _cco.CapSecurityFlapForward; }
        }

        public override int CapSpeedStepsBackward
        {
            get { return _cco.CapSpeedStepsBackward; }
        }

        public override int CapSpeedStepsForward
        {
            get { return _cco.CapSpeedStepsForward; }
        }

        public override bool AutoStopBackward
        {
            get
            {
                return _cco.AutoStopBackward;
            }
            set
            {
                _cco.AutoStopBackward = value;
                VerifyResult(_cco.ResultCode);
            }
        }

        public override int AutoStopBackwardDelayTime
        {
            get
            {
                return _cco.AutoStopBackwardDelayTime;
            }
            set
            {
                _cco.AutoStopBackwardDelayTime = value;
                VerifyResult(_cco.ResultCode);
            }
        }

        public override int AutoStopBackwardItemCount
        {
            get { return _cco.AutoStopBackwardItemCount; }
        }

        public override bool AutoStopForward
        {
            get
            {
                return _cco.AutoStopForward;
            }
            set
            {
                _cco.AutoStopForward = value;
                VerifyResult(_cco.ResultCode);
            }
        }

        public override int AutoStopForwardDelayTime
        {
            get
            {
                return _cco.AutoStopForwardDelayTime;
            }
            set
            {
                _cco.AutoStopForwardDelayTime = value;
                VerifyResult(_cco.ResultCode);
            }
        }

        public override int AutoStopForwardItemCount
        {
            get { return _cco.AutoStopForwardItemCount; }
        }

        public override bool LightBarrierBackwardInterrupted
        {
            get { return _cco.LightBarrierBackwardInterrupted; }
        }

        public override bool LightBarrierForwardInterrupted
        {
            get { return _cco.LightBarrierForwardInterrupted; }
        }

        public override BeltMotionStatus MotionStatus
        {
            get { return (BeltMotionStatus)InteropEnum<BeltMotionStatus>.ToEnumFromInteger(_cco.MotionStatus); }
        }

        public override bool SecurityFlapBackwardOpened
        {
            get { return _cco.SecurityFlapBackwardOpened; }
        }

        public override bool SecurityFlapForwardOpened
        {
            get { return _cco.SecurityFlapForwardOpened; }
        }

        #endregion OPOSBelt Specific Properties

        #region OPOSBelt Specific Methodss

        public override void AdjustItemCount(BeltDirection direction, int count)
        {
            VerifyResult(_cco.AdjustItemCount((int)direction, count));
        }

        public override void MoveBackward(int speed)
        {
            VerifyResult(_cco.MoveBackward(speed));
        }

        public override void MoveForward(int speed)
        {
            VerifyResult(_cco.MoveForward(speed));
        }

        public override void ResetBelt()
        {
            VerifyResult(_cco.ResetBelt());
        }

        public override void ResetItemCount(BeltDirection direction)
        {
            VerifyResult(_cco.ResetItemCount((int)direction));
        }

        public override void StopBelt()
        {
            VerifyResult(_cco.StopBelt());
        }

        #endregion OPOSBelt Specific Methodss
    }
}