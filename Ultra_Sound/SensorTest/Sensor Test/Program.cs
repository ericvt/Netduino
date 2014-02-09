using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace Sendor_Test
{
    public class HC_SR04
    {
        private OutputPort trigOut;
        public OutputPort LEDOut = new OutputPort(Pins.GPIO_PIN_D0, false); //buzzer
        public OutputPort LEDRed = new OutputPort(Pins.GPIO_PIN_D8, false); 
        public OutputPort LEDGreen = new OutputPort(Pins.GPIO_PIN_D9, false);
        public OutputPort LEDBlue = new OutputPort(Pins.GPIO_PIN_D10, false);
        private InterruptPort EchoIn;
        private long beginTick;
        private long endTick;
        private long minTicks;  // System latency, subtracted off ticks to find actual sound travel time
        private double inchConversion;
   //     private double cmConversion;

        /// <param name="pinTrig">Netduino Trig pin</param>
        /// <param name="pinEcho">Netduino Echo pin</param>
        public HC_SR04(Cpu.Pin pinTrig, Cpu.Pin pinEcho)
        {
            trigOut = new OutputPort(pinTrig, false);
            EchoIn = new InterruptPort(pinEcho, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
            EchoIn.OnInterrupt += new NativeEventHandler(EchoIn_OnInterrupt);
            minTicks = 6200L;
            inchConversion = 1440;
      //      cmConversion = 567;
        }

        /// Trigger a sensor reading
        /// Convert ticks to distance using TicksToInches below
        /// <returns>Number of ticks it takes to get back sonic pulse</returns>
        public long Ping()
        {
            // Reset Sensor
            trigOut.Write(true);
            Thread.Sleep(1);
            // Start Clock
            endTick = 0L;
            beginTick = System.DateTime.Now.Ticks;
            // Trigger Pulse
            trigOut.Write(false);
            Thread.Sleep(50);  // Wait 1/20 second (this could be set as a variable instead of constant)

            //verify interupt state
            if (endTick > 0L)
            {
                // Calculate Difference
                long elapsed = endTick - beginTick;

                // Subtract out fixed overhead (interrupt lag, etc.)
                elapsed -= minTicks;
                if (elapsed < 0L)
                {
                    elapsed = 0L;
                }

                // Return elapsed ticks
                return elapsed;
            }
            // Sonic pulse wasn't detected within 1/20 second
            return -1L;
        }

        /// <summary>
        /// interrupt triggered when detector receives back pulse       
        /// </summary>
        /// <param name="time">Transfer to endTick to calculated sound pulse travel time</param>
        void EchoIn_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            endTick = time.Ticks;  // Save the ticks when pulse was received back
          
        }

        /// <summary>
        /// Convert ticks to inches
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns></returns>
        public double TicksToInches(long ticks)
        {
            return (double)ticks / inchConversion;
        }

        /// <summary>
        /// The system latency (minimum number of ticks)
        /// This number will be subtracted off to find actual sound travel time
        /// </summary>
        public long LatencyTicks
        {
            get {return minTicks;}
            set {minTicks = value;}
        }
    }

    public class Program
    {
        public static void Main()
        {
            //arg1= pinTrig, pinEcho
            HC_SR04 _capteur = new HC_SR04(Pins.GPIO_PIN_D6, Pins.GPIO_PIN_D7);
            double interval;
       
            while (true)
            {
                long ticks = _capteur.Ping();
                _capteur.LEDOut.Write(false);
                if (ticks > 0L)
                {
                    double inches = _capteur.TicksToInches(ticks);
                   Microsoft.SPOT.Debug.Print(inches.ToString());

                   if (inches < 100L )
                   {
                       if (inches < 50L) /// - ENZO change ici ///
                       {
                           interval = 30 * inches;
                           _capteur.LEDOut.Write(true); _capteur.LEDRed.Write(true); 
                           Thread.Sleep((int)interval); _capteur.LEDOut.Write(false);
                           _capteur.LEDRed.Write(false);
                       }
                     /*  if (inches > 5L && inches < 20L)
                       {
                           _capteur.LEDGreen.Write(true); Thread.Sleep((int)interval); _capteur.LEDGreen.Write(false);
                       }
                       if (inches > 20L)
                       {
                           _capteur.LEDBlue.Write(true);
                       }*/
                   }
                   else
                   {
                       _capteur.LEDOut.Write(false);
                       _capteur.LEDRed.Write(false);
                       _capteur.LEDGreen.Write(false);
                       _capteur.LEDBlue.Write(false);
                   }
                  
                }
            }
        }
    }
}
