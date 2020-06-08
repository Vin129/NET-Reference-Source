using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input.StylusWisp;
using System.Windows.Input.Tracing;
using System.Windows.Media;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using System.Security.Permissions;
using MS.Win32.Penimc;
using MS.Win32;


namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Class that represents a physical digitizer connected to the system.
    ///     Tablets are the source of events for the Stylus devices.
    /// </summary>
    internal class WispTabletDevice : TabletDeviceBase
    {
        /////////////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////////
        /// <SecurityNote>
        ///     Critical:  creates security critical data (IPimcTablet2)
        /// </SecurityNote>
        [SecurityCritical]
        internal WispTabletDevice(TabletDeviceInfo tabletInfo, PenThread penThread)
            : base(tabletInfo)
        {
            _penThread = penThread;

            // DDVSO:514949
            // Constructing a WispTabletDevice means we will actually use this tablet for input purposes.
            // Lock the tablet and underlying WISP tablet at this point.
            // This is balanced in DisposeOrDeferDisposal.
            _penThread.WorkerAcquireTabletLocks(tabletInfo.PimcTablet.Value, tabletInfo.WispTabletKey);

            int count = tabletInfo.StylusDevicesInfo.Length;

            WispStylusDevice[] styluses = new WispStylusDevice[count];
            for (int i = 0; i < count; i++)
            {
                StylusDeviceInfo cursorInfo = tabletInfo.StylusDevicesInfo[i];
                styluses[i] = new WispStylusDevice(
                    this,
                    cursorInfo.CursorName,
                    cursorInfo.CursorId,
                    cursorInfo.CursorInverted,
                    cursorInfo.ButtonCollection);
            }

            _stylusDeviceCollection = new StylusDeviceCollection(styluses);

            // We only create a TabletDevice when one is connected (physically or virtually).
            // So we can log the connection in the constructor.
            StylusTraceLogger.LogDeviceConnect(
                new StylusTraceLogger.StylusDeviceInfo(
                    Id,
                    Name,
                    ProductId,
                    TabletHardwareCapabilities,
                    TabletSize,
                    ScreenSize,
                    _tabletInfo.DeviceType,
                    StylusDevices.Count));
        }

        /// <summary>
        /// Dispose pattern override from base
        /// </summary>
        /// <param name="disposing">True if disposing, false if finalizing</param>
        [SecurityCritical]
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // If this is an explicit dispose, need to check for deferral
                    DisposeOrDeferDisposal();
                }
                else
                {
                    _disposed = true;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <SecurityNote>
        ///     Critical: - Calls unmanaged code that is SecurityCritical with SUC attribute.
        ///                 - accesses security critical data _pimcTablet.Value
        ///           - called by constructor
        /// </SecurityNote>
        [SecurityCritical]
        internal WispStylusDevice UpdateStylusDevices(int stylusId)
        {

            // REENTRANCY NOTE: Use PenThread to talk to wisptis.exe to make sure we are not reentrant!
            //                  PenImc will cache all the stylus device info so we don't have 
            //                  any Out of Proc calls to wisptis.exe to get this info.

            StylusDeviceInfo[] stylusDevicesInfo = _penThread.WorkerRefreshCursorInfo(_tabletInfo.PimcTablet.Value);

            int cCursors = stylusDevicesInfo.Length;

            if (cCursors > StylusDevices.Count)
            {
                for (int iCursor = 0; iCursor < cCursors; iCursor++)
                {
                    StylusDeviceInfo stylusDeviceInfo = stylusDevicesInfo[iCursor];

                    // See if we found it.  If so go and create the new StylusDevice and add it.
                    if (stylusDeviceInfo.CursorId == stylusId)
                    {
                        WispStylusDevice newStylusDevice = new WispStylusDevice(
                                                                this,
                                                                stylusDeviceInfo.CursorName,
                                                                stylusDeviceInfo.CursorId,
                                                                stylusDeviceInfo.CursorInverted,
                                                                stylusDeviceInfo.ButtonCollection);
                        StylusDevices.AddStylusDevice(iCursor, newStylusDevice);

                        return newStylusDevice;
                    }
                }

            }

            return null;  // Nothing to add
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Returns the element that input from this device is sent to.
        /// </summary>
        internal override IInputElement Target
        {
            get
            {
                VerifyAccess();
                StylusDevice stylusDevice = Stylus.CurrentStylusDevice;
                if (stylusDevice == null)
                    return null;
                return stylusDevice.Target;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Returns the PresentationSource that is reporting input for this device.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        /// <SecurityNote>
        /// Critical - accesses critical data (InputSource on StylusDevice)
        /// PublicOK - there is a demand.
        ///               this is safe as: 
        ///                     there is a demand for the UI permissions in the code
        /// </SecurityNote>
        internal override PresentationSource ActiveSource
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUIWindowPermission();
                VerifyAccess();
                StylusDevice stylusDevice = Stylus.CurrentStylusDevice;
                if (stylusDevice == null)
                    return null;
                return stylusDevice.ActiveSource;  // This also does a security demand.
            }
        }

        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Returns the friendly string representation of the Tablet object
        /// </summary>
        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}({1})", base.ToString(), Name);
        }

        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Returns the collection of StylusDevices defined on this tablet.
        ///     An Empty collection is returned if the device has not seen any Stylus Pointers.
        /// </summary>
        internal override StylusDeviceCollection StylusDevices
        {
            get
            {
                VerifyAccess();

                Debug.Assert(_stylusDeviceCollection != null);
                return _stylusDeviceCollection;
            }
        }

        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        /// <SecurityNote>
        ///     Critical: - calls into unmanaged code that is SecurityCritical with SUC attribute.
        ///                 - accesses security critical data _pimcTablet.Value
        ///                 - takes in data that is potential security risk (hwnd)
        /// </SecurityNote>
        [SecurityCritical]
        internal PenContext CreateContext(IntPtr hwnd, PenContexts contexts)
        {
            PenContext penContext;
            bool supportInRange = (this.TabletHardwareCapabilities & TabletHardwareCapabilities.HardProximity) != 0;
            bool isIntegrated = (this.TabletHardwareCapabilities & TabletHardwareCapabilities.Integrated) != 0;

            // Use a PenThread to create a tablet context so we don't cause reentrancy.
            PenContextInfo result = _penThread.WorkerCreateContext(hwnd, _tabletInfo.PimcTablet.Value);

            penContext = new PenContext(result.PimcContext != null ? result.PimcContext.Value : null,
                                        hwnd, contexts,
                                        supportInRange, isIntegrated, result.ContextId,
                                        result.CommHandle != null ? result.CommHandle.Value : IntPtr.Zero,
                                        Id, result.WispContextKey);
            return penContext;
        }


        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        /// <SecurityNote>
        ///     Critical: - returns data that is potential a security risk (PenThread)
        /// </SecurityNote>
        internal PenThread PenThread
        {
            [SecurityCritical]
            get
            {
                return _penThread;
            }
        }

        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        /// <SecurityNote>
        ///     Critical: - calls into unmanaged code that is SecurityCritical with SUC attribute.
        ///               - accesses security critical data _pimcTablet.Value
        /// </SecurityNote>
        [SecurityCritical]
        internal void UpdateScreenMeasurements()
        {
            Debug.Assert(CheckAccess());

            // Update and reset these sizes to be recalculated the next time they are needed.
            _cancelSize = Size.Empty;
            _doubleTapSize = Size.Empty;

            // Update the size info we use to map tablet coordinates to screen coordinates.
            _tabletInfo.SizeInfo = _penThread.WorkerGetUpdatedSizes(_tabletInfo.PimcTablet.Value);
        }

        // NOTE: UpdateSizeDeltas MUST be called before the returned Size is valid.
        //  Since we currently only call this while processing Down stylus events
        //  after StylusDevice.UpdateState has been called it will always be initialized appropriately.
        internal override Size DoubleTapSize
        {
            get
            {
                return _doubleTapSize;   // used for double tap detection - updating click count.
            }
        }

        // NOTE: UpdateSizeDeltas MUST be called before the returned Size is valid.
        //  Since we currently only call this while processing Move stylus events
        //  after StylusDevice.UpdateState has been called it will always be initialized appropriately.
        internal Size CancelSize
        {
            get
            {
                return _cancelSize; // Used for drag detection when double tapping.
            }
        }

        // Forces the UpdateSizeDeltas to re-calc the sizes the next time it is called.
        // NOTE: We don't invalidate the sizes and just leave them till the next time we
        //  UpdateSizeDeltas gets called.
        internal void InvalidateSizeDeltas()
        {
            _forceUpdateSizeDeltas = true;
        }

        internal bool AreSizeDeltasValid()
        {
            return !(_doubleTapSize.IsEmpty || _cancelSize.IsEmpty);
        }

        /// <summary>
        /// Updates the various size parameters for drag/drop/tap.
        /// </summary>
        /// <param name="description"></param>
        /// <param name="stylusLogic"></param>
        /// <SecurityNote>
        ///      Critical as this uses SecurityCritical _stylusLogic variable.
        ///
        ///      At the top called from StylusLogic::PreProcessInput event which is SecurityCritical
        /// </SecurityNote>
        [SecurityCritical]
        internal void UpdateSizeDeltas(StylusPointDescription description, WispLogic stylusLogic)
        {
            // Query default settings for mouse drag and double tap (with minimum of 1x1 size).
            Size mouseDragDefault = new Size(Math.Max(1, SafeSystemMetrics.DragDeltaX / 2),
                                           Math.Max(1, SafeSystemMetrics.DragDeltaY / 2));
            Size mouseDoubleTapDefault = new Size(Math.Max(1, SafeSystemMetrics.DoubleClickDeltaX / 2),
                                           Math.Max(1, SafeSystemMetrics.DoubleClickDeltaY / 2));

            StylusPointPropertyInfo xProperty = description.GetPropertyInfo(StylusPointProperties.X);
            StylusPointPropertyInfo yProperty = description.GetPropertyInfo(StylusPointProperties.Y);

            uint dwXValue = GetPropertyValue(xProperty);
            uint dwYValue = GetPropertyValue(yProperty);

            if (dwXValue != 0 && dwYValue != 0)
            {
                _cancelSize = new Size((int)Math.Round((ScreenSize.Width * stylusLogic.CancelDelta) / dwXValue),
                                       (int)Math.Round((ScreenSize.Height * stylusLogic.CancelDelta) / dwYValue));

                // Make sure we return whole numbers (pixels are whole numbers) and take the maximum
                // value between mouse and stylus settings to be safe.
                _cancelSize.Width = Math.Max(mouseDragDefault.Width, _cancelSize.Width);
                _cancelSize.Height = Math.Max(mouseDragDefault.Height, _cancelSize.Height);

                _doubleTapSize = new Size((int)Math.Round((ScreenSize.Width * stylusLogic.DoubleTapDelta) / dwXValue),
                                          (int)Math.Round((ScreenSize.Height * stylusLogic.DoubleTapDelta) / dwYValue));

                // Make sure we return whole numbers (pixels are whole numbers) and take the maximum
                // value between mouse and stylus settings to be safe.
                _doubleTapSize.Width = Math.Max(mouseDoubleTapDefault.Width, _doubleTapSize.Width);
                _doubleTapSize.Height = Math.Max(mouseDoubleTapDefault.Height, _doubleTapSize.Height);
            }
            else
            {
                // If no info to do the calculation then use the mouse settings for the default.
                _doubleTapSize = mouseDoubleTapDefault;
                _cancelSize = mouseDragDefault;
            }

            _forceUpdateSizeDeltas = false;
        }

        /// <summary>
        /// DevDiv:1078091
        /// It's possible that there is pending input on the stylus queue for this tablet.
        /// In such cases, we cannot immediately call Dispose as future input needs this 
        /// object's data.  Instead, set a flag that indicates we should dispose of this 
        /// tablet once all input has been processed.
        /// </summary>
        /// <SecurityNote>
        ///     Critical:  Calls StylusDeviceCollection.Dispose
        ///                      StylusLogic.GetCurrentStylusLogicAs
        ///                      Marshal.ReleaseComObject
        /// </SecurityNote>
        [SecurityCritical]
        internal void DisposeOrDeferDisposal()
        {
            // Only dispose when no input events are left in the queue
            if (CanDispose)
            {
                // Make sure this device is not the current one.
                if (Tablet.CurrentTabletDevice == this.TabletDevice)
                {
                    StylusLogic.GetCurrentStylusLogicAs<WispLogic>().SelectStylusDevice(null, null, true);
                }

                // A disconnect will be logged in the dispose as WPF will have gotten rid of the tablet.
                StylusTraceLogger.LogDeviceDisconnect(_tabletInfo.Id);

                // DDVSO:174153
                // Force tablets to clean up as soon as they are disposed.  This helps to reduce
                // COM references that might be waiting for RCWs to finalize.
                IPimcTablet2 tablet = _tabletInfo.PimcTablet?.Value;
                _tabletInfo.PimcTablet = null;

                if (tablet != null)
                {
                    // DDVSO:514949
                    // Balance calls in PenThreadWorker.GetTabletInfoHelper and CPimcTablet::Init.
                    PenThread.WorkerReleaseTabletLocks(tablet, _tabletInfo.WispTabletKey);
                    
                    Marshal.ReleaseComObject(tablet);
                }

                StylusDeviceCollection styluses = _stylusDeviceCollection;
                _stylusDeviceCollection = null;

                if (styluses != null)
                {
                    styluses.Dispose();
                }

                _penThread = null;
                _isDisposalPending = false;

                // DDVSO:614343
                // Ensure that we are marked disposed and no longer attempt to finalize.
                _disposed = true;
                GC.SuppressFinalize(this);
            }
            else
            {
                _isDisposalPending = true;
            }
        }

        /// <summary>
        /// DevDiv:1078091
        /// This flag will be set if we receive a removal notification while there is pending
        /// input in the stylus queue.  This will then indicate that, at some point when input
        /// is fully processed, we should dispose the device.
        /// </summary>
        internal bool IsDisposalPending
        {
            get
            {
                return _isDisposalPending;
            }
        }

        /// <summary>
        /// DevDiv:1078091
        /// Indicates if immediate disposal is possible.  We can't dispose if there
        /// are any messages waiting in the queue or if there is still an active stylus device.
        /// </summary>
        internal bool CanDispose
        {
            get
            {
                return _queuedEventCount == 0;
            }
        }

        /// <summary>
        /// DevDiv:1078091
        /// Maintain a count of how many items are pushed on the stylus input queue to guard
        /// against disposing a TabletDevice prematurely.
        /// </summary>
        internal int QueuedEventCount
        {
            get
            {
                return _queuedEventCount;
            }

            set
            {
                _queuedEventCount = value;
            }
        }

        /// <summary>
        /// The GIT key for the WISP tablet COM object.
        /// </summary>
        internal UInt32 WispTabletKey
        {
            [SecurityCritical]
            get
            {
                return _tabletInfo.WispTabletKey;
            }
        }

        /// <SecurityNote>
        ///     Critical to prevent accidental spread to transparent code
        /// </SecurityNote>
        [SecurityCritical]
        PenThread _penThread; // Hold ref on worker thread we use to talk to wisptis.

        protected Size _cancelSize = Size.Empty;

        StylusDeviceCollection _stylusDeviceCollection;

        private bool _isDisposalPending;

        private int _queuedEventCount = 0;
    }
}