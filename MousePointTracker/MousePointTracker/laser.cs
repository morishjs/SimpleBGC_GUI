using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;
using System.Timers;

namespace MousePointTracker
{
    public class laser
    {
        private static bool shotAvailable = true;
        private static System.Timers.Timer aTimer;

        public bool checkShot()
        {            
            return shotAvailable;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            shotAvailable = true;
        }

        public void shot()
        {            
            try
            {
                using (Task myTask = new Task())
                {
                    if (checkShot()) { 
                        myTask.AOChannels.CreateVoltageChannel("Dev1", "aoChannel",
                           Convert.ToDouble(-10), Convert.ToDouble(10),
                           AOVoltageUnits.Volts);

                        AnalogSingleChannelWriter writer = new AnalogSingleChannelWriter(myTask.Stream);

                        writer.WriteSingleSample(true, Convert.ToDouble(0));
                        System.Threading.Thread.Sleep(1);
                        writer.WriteSingleSample(true, Convert.ToDouble(5));
                        System.Threading.Thread.Sleep(1);
                        writer.WriteSingleSample(true, Convert.ToDouble(0));
                        shotAvailable = false;

                        //Timer On
                        // Create a timer with 2.5 sec interval.
                        aTimer = new System.Timers.Timer(2500);
                        // Hook up the Elapsed event for the timer. 
                        aTimer.Elapsed += OnTimedEvent;
                        aTimer.AutoReset = false;
                        aTimer.Enabled = true;
                    }
                }
            }
            catch (DaqException ex)
            {
                
            }
        }
    }
}
