using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
using System.Windows.Forms;


using IpcContractClientInterface;
using AppLog = IpcUtil.Logging;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace Tutorial1_XraysOnOff
{
    public partial class UserForm : Form
    {
        /// <summary>Are we in design mode</summary>
        protected bool mDesignMode { get; private set; }

        #region Standard IPC Variables

        /// <summary>This ensures consistent read and write culture</summary>
        private NumberFormatInfo mNFI = new CultureInfo("en-GB", true).NumberFormat; // Force UN English culture

        /// <summary>Collection of all IPC channels, this object always exists.</summary>
        private Channels mChannels = new Channels();

        #endregion Standard IPC Variables

        #region Application Variables

        /// <summary> Status of the application </summary>
        private Channels.EConnectionState mApplicationState;

        /// <summary> Flag for X-ray stability </summary>
        private Boolean mXraysStable = false;

        /// <summary> Thread for X-ray Routine </summary>
        private Thread mXrayRoutineThread = null;

        /// <summary> Entire Xray Status (for bug correction) </summary>
        private IpcContract.XRay.EntireStatus mXrayEntireStatus;

        /// <summary> Generation status </summary>
        private IpcContract.XRay.GenerationStatus.EXRayGenerationState mXrayGenerationStatus;

        /// <summary> Stability event counter </summary>
        private int mXraysStabilityCounter = 0;

        #endregion Application Variables

        public UserForm()
        {
            try
            {
                mDesignMode = (LicenseManager.CurrentContext.UsageMode == LicenseUsageMode.Designtime);
                InitializeComponent();
                if (!mDesignMode)
                {
                    // Tell normal logging who the parent window is.
                    AppLog.SetParentWindow = this;
                    AppLog.TraceInfo = true;
                    AppLog.TraceDebug = true;

                    mChannels = new Channels();
                    // Enable the channels that will be controlled by this application.
                    // For the generic IPC client this is all of them!
                    // This just sets flags, it does not actually open the channels.
                    mChannels.AccessApplication = true;
                    mChannels.AccessXray = true;
                    mChannels.AccessManipulator = false;
                    mChannels.AccessImageProcessing = false;
                    mChannels.AccessInspection = false;
                    mChannels.AccessInspection2D = false;
                    mChannels.AccessCT3DScan = false;
                    mChannels.AccessCT2DScan = false;
                }
            }
            catch (Exception ex) { AppLog.LogException(ex); }
        }

        #region Channel connections

        /// <summary>Attach to channel and connect any event handlers</summary>
        /// <returns>Connection status</returns>
        private Channels.EConnectionState ChannelsAttach()
        {
            try
            {
                if (mChannels != null)
                {
                    Channels.EConnectionState State = mChannels.Connect();
                    if (State == Channels.EConnectionState.Connected)  // Open channels
                    {
                        // Attach event handlers (as required)

                        if (mChannels.Application != null)
                        {
                            mChannels.Application.mEventSubscriptionHeartbeat.Event +=
                                new EventHandler<CommunicationsChannel_Application.EventArgsHeartbeat>(EventHandlerHeartbeatApp);
                        }

                        if (mChannels.Xray != null)
                        {
                            mChannels.Xray.mEventSubscriptionHeartbeat.Event +=
                                new EventHandler<CommunicationsChannel_XRay.EventArgsHeartbeat>(EventHandlerHeartbeatXRay);
                            mChannels.Xray.mEventSubscriptionEntireStatus.Event +=
                                new EventHandler<CommunicationsChannel_XRay.EventArgsXRayEntireStatus>(EventHandlerXRayEntireStatus);
                        }

                    }
                    return State;
                }
            }
            catch (Exception ex) { AppLog.LogException(ex); }
            return Channels.EConnectionState.Error;
        }

        /// <summary>Detach channel and disconnect any event handlers</summary>
        /// <returns>true if OK</returns>
        private bool ChannelsDetach()
        {
            try
            {
                if (mChannels != null)
                {
                    // Detach event handlers

                    if (mChannels.Application != null)
                    {
                        mChannels.Application.mEventSubscriptionHeartbeat.Event -=
                            new EventHandler<CommunicationsChannel_Application.EventArgsHeartbeat>(EventHandlerHeartbeatApp);
                    }

                    if (mChannels.Xray != null)
                    {
                        mChannels.Xray.mEventSubscriptionHeartbeat.Event -=
                            new EventHandler<CommunicationsChannel_XRay.EventArgsHeartbeat>(EventHandlerHeartbeatXRay);
                        mChannels.Xray.mEventSubscriptionEntireStatus.Event -=
                            new EventHandler<CommunicationsChannel_XRay.EventArgsXRayEntireStatus>(EventHandlerXRayEntireStatus);
                    }

                    Thread.Sleep(100); // A breather for events to finish!
                    return mChannels.Disconnect(); // Close channels
                }
            }
            catch (Exception ex) { AppLog.LogException(ex); }
            return false;
        }

        #endregion Channel connections

        #region Heartbeat from host

        void EventHandlerHeartbeatApp(object aSender, CommunicationsChannel_Application.EventArgsHeartbeat e)
        {
            try
            {
                if (mChannels == null || mChannels.Application == null)
                    return;
                if (this.InvokeRequired)
                    this.BeginInvoke((MethodInvoker)delegate { EventHandlerHeartbeatApp(aSender, e); });
                else
                {
                    //your code goes here....
                }
            }
            catch (ObjectDisposedException) { } // ignore
            catch (Exception ex) { AppLog.LogException(ex); }
        }

        void EventHandlerHeartbeatXRay(object aSender, CommunicationsChannel_XRay.EventArgsHeartbeat e)
        {
            try
            {
                if (mChannels == null || mChannels.Xray == null)
                    return;
                if (this.InvokeRequired)
                    this.BeginInvoke((MethodInvoker)delegate { EventHandlerHeartbeatXRay(aSender, e); });
                else
                {
                    //your code goes here....
                }
            }
            catch (ObjectDisposedException) { } // ignore
            catch (Exception ex) { AppLog.LogException(ex); }
        }

        void EventHandlerHeartbeatMan(object aSender, CommunicationsChannel_Manipulator.EventArgsHeartbeat e)
        {
            try
            {
                if (mChannels == null || mChannels.Manipulator == null)
                    return;
                if (this.InvokeRequired)
                    this.BeginInvoke((MethodInvoker)delegate { EventHandlerHeartbeatMan(aSender, e); });
                else
                {
                    //your code goes here....
                }
            }
            catch (ObjectDisposedException) { } // ignore
            catch (Exception ex) { AppLog.LogException(ex); }
        }

        void EventHandlerHeartbeatIP(object aSender, CommunicationsChannel_ImageProcessing.EventArgsHeartbeat e)
        {
            try
            {
                if (mChannels == null || mChannels.ImageProcessing == null)
                    return;
                if (this.InvokeRequired)
                    this.BeginInvoke((MethodInvoker)delegate { EventHandlerHeartbeatIP(aSender, e); });
                else
                {
                    //your code goes here...
                }
            }
            catch (ObjectDisposedException) { } // ignore
            catch (Exception ex) { AppLog.LogException(ex); }
        }

        void EventHandlerHeartbeatInspection(object aSender, CommunicationsChannel_Inspection.EventArgsHeartbeat e)
        {
            try
            {
                if (mChannels == null || mChannels.Inspection == null)
                    return;
                if (this.InvokeRequired)
                    this.BeginInvoke((MethodInvoker)delegate { EventHandlerHeartbeatInspection(aSender, e); });
                else
                {
                    //your code goes here....
                }
            }
            catch (ObjectDisposedException) { } // ignore
            catch (Exception ex) { AppLog.LogException(ex); }
        }

        void EventHandlerHeartbeatInspection2D(object aSender, CommunicationsChannel_Inspection2D.EventArgsHeartbeat e)
        {
            try
            {
                if (mChannels == null || mChannels.Inspection2D == null)
                    return;
                if (this.InvokeRequired)
                    this.BeginInvoke((MethodInvoker)delegate { EventHandlerHeartbeatInspection2D(aSender, e); });
                else
                {
                    //your code goes here....
                }
            }
            catch (ObjectDisposedException) { } // ignore
            catch (Exception ex) { AppLog.LogException(ex); }
        }

        void EventHandlerHeartbeatCT3DScan(object aSender, CommunicationsChannel_CT3DScan.EventArgsHeartbeat e)
        {
            try
            {
                if (mChannels == null || mChannels.CT3DScan == null)
                    return;
                if (this.InvokeRequired)
                    this.BeginInvoke((MethodInvoker)delegate { EventHandlerHeartbeatCT3DScan(aSender, e); });
                else
                {
                    //your code goes here....
                }
            }
            catch (ObjectDisposedException) { } // ignore
            catch (Exception ex) { AppLog.LogException(ex); }
        }

        void EventHandlerHeartbeatCT2DScan(object aSender, CommunicationsChannel_CT2DScan.EventArgsHeartbeat e)
        {
            try
            {
                if (mChannels == null || mChannels.CT2DScan == null)
                    return;
                if (this.InvokeRequired)
                    this.BeginInvoke((MethodInvoker)delegate { EventHandlerHeartbeatCT2DScan(aSender, e); });
                else
                {
                    //your code goes here....
                }
            }
            catch (ObjectDisposedException) { } // ignore
            catch (Exception ex) { AppLog.LogException(ex); }
        }

        #endregion Heartbeat from host

        #region STATUS FROM HOST

        #region XRay

        void EventHandlerXRayEntireStatus(object aSender, CommunicationsChannel_XRay.EventArgsXRayEntireStatus e)
        {
            try
            {
                if (mChannels == null || mChannels.Xray == null)
                    return;
                if (this.InvokeRequired)
                    this.BeginInvoke((MethodInvoker)delegate { EventHandlerXRayEntireStatus(aSender, e); }); // Make it non blocking if called form this UI thread
                else
                {
                    if (e.EntireStatus != null)
                    {

                        Debug.Print(DateTime.Now.ToString("dd/MM/yyyy H:mm:ss.fff") + " : e.EntireStatus.XRaysStatus.GenerationStatus.State=" + e.EntireStatus.XRaysStatus.GenerationStatus.State.ToString());

                        switch (e.EntireStatus.XRaysStatus.GenerationStatus.State)
                        {
                            case IpcContract.XRay.GenerationStatus.EXRayGenerationState.Success:
                                // Set mXraysStable Flag to true indicating stability has been reached
                                mXraysStable = true;
                                break;
                            case IpcContract.XRay.GenerationStatus.EXRayGenerationState.WaitingForStability:
                                // Increment stability counter;
                                mXraysStabilityCounter++;

                                // If stability counter is greater than 1 then must manually check update X-ray Entire status
                                if (mXraysStabilityCounter > 1)
                                {
                                    // Manual loop to update X-ray Entire Status until "Success"

                                    Debug.Print(DateTime.Now.ToString("dd/MM/yyyy H:mm:ss.fff") + " : Manually checking for stability");

                                    do
                                    {
                                        // First sleep for a small amount of time to allow status updates
                                        Thread.Sleep(100);
                                        // Then get a updated X-ray Entire Status
                                        mXrayEntireStatus = mChannels.Xray.GetXRayEntireStatus();
                                        // Find generation part of Entire Status
                                        mXrayGenerationStatus = mXrayEntireStatus.XRaysStatus.GenerationStatus.State;

                                        Debug.Print(DateTime.Now.ToString("dd/MM/yyyy H:mm:ss.fff") + " : mXrayGenerationStatus=" + mXrayGenerationStatus.ToString());
                                    }
                                    while (mXrayGenerationStatus != IpcContract.XRay.GenerationStatus.EXRayGenerationState.Success);

                                    Debug.Print(DateTime.Now.ToString("dd/MM/yyyy H:mm:ss.fff") + " : Manually found stable X-rays- Proceed");

                                    // Once "Success" obtained then set mXraysStable flag to true
                                    mXraysStable = true;

                                    // Reset stability counter
                                    mXraysStabilityCounter = 0;
                                }
                                break;
                            case IpcContract.XRay.GenerationStatus.EXRayGenerationState.NoXRayController:
                                // Your code goes here...
                                break;
                            case IpcContract.XRay.GenerationStatus.EXRayGenerationState.StabilityTimeout:
                                // Your code goes here...
                                break;
                            case IpcContract.XRay.GenerationStatus.EXRayGenerationState.StabilityXRays:
                                // Your code goes here...
                                break;
                            case IpcContract.XRay.GenerationStatus.EXRayGenerationState.SwitchedOff:
                                // Set reset flag when X-rays are turned off
                                mXraysStable = false;
                                break;
                        }

                        Debug.Print(DateTime.Now.ToString("dd/MM/yyyy H:mm:ss.fff") + " : mXraysStable=" + mXraysStable.ToString());
                    }
                }
            }
            catch (Exception ex) { AppLog.LogException(ex); }
        }


        #endregion

        #endregion Status from host


        #region Form Functions

        private void UserForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Attach channels
                mApplicationState = ChannelsAttach();

                if (mApplicationState == Channels.EConnectionState.Connected)
                    Debug.Print(DateTime.Now.ToString("dd/MM/yyyy H:mm:ss.fff") + " : Connected to Inspect-X");
                else
                    Debug.Print(DateTime.Now.ToString("dd/MM/yyyy H:mm:ss.fff") + " : Problem in connecting to Inspect-X");
            }
            catch (Exception ex) { AppLog.LogException(ex); }
        }



        private void UserForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // Detach channels
                ChannelsDetach();

                Debug.Print(DateTime.Now.ToString("dd/MM/yyyy H:mm:ss.fff") + " : Disconnected from Inspect-X");
            }
            catch (Exception ex) { AppLog.LogException(ex); }
        }

        private void btn_Start_Click(object sender, EventArgs e)
        {
            // Assign the XrayRoutine to the mXrayRoutineThread
            mXrayRoutineThread = new Thread(XrayRoutine);

            // Start the thread
            mXrayRoutineThread.Start();
        }

        #endregion Form Functions

        #region X-ray functions

        private void XrayRoutine()
        {
            // If ApplicationState is not connected then immediately exit the routine
            if (mApplicationState != Channels.EConnectionState.Connected)
                return;

            // For safety, disable the Start button
            this.Invoke((MethodInvoker)delegate { btn_Start.Enabled = false; });

            // Set mXraysStable flag to false
            mXraysStable = false;

            // Turn the X-rays on. 
            mChannels.Xray.XRays.GenerationDemand(true);

            // Wait until X-rays have stabilised
            while (!mXraysStable)
                Thread.Sleep(5);

            // Once stable, wait for a further 5 seconds
            Thread.Sleep(5000);

            // Turn the X-rays off
            mChannels.Xray.XRays.GenerationDemand(false);

            // Wait until X-rays have turned off
            while (mXraysStable)
                Thread.Sleep(5);

            // Re-enable the Start button
            this.Invoke((MethodInvoker)delegate { btn_Start.Enabled = true; });
        }

        #endregion X-ray functions



    }
}