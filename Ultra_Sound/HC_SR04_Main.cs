using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace SonarApp
{
    public class Program
    {
        public static DateTime startTime;        
        static int timeOut = 10;
        static uint bufferSize = 17;
        static uint size = 4;
        static byte[][] faces;
        static I2CDevice matrix8x8;
        static I2CDevice.I2CWriteTransaction st;
        static PWM sound;

        public static void Main()
        { 
            I2CDevice.Configuration i2cConfig = new I2CDevice.Configuration(0x70, 100);
            matrix8x8 = new I2CDevice(i2cConfig);

            st = I2CDevice.CreateWriteTransaction(new byte[] { 0x21 }); // start command
            matrix8x8.Execute(new I2CDevice.I2CTransaction[] { st }, timeOut);

            st = I2CDevice.CreateWriteTransaction(new byte[] { 0x80 | 0x01 | 0x00 });
            matrix8x8.Execute(new I2CDevice.I2CTransaction[] { st }, timeOut);

            st = I2CDevice.CreateWriteTransaction(new byte[] { 0xE0 | 0x0F });
            matrix8x8.Execute(new I2CDevice.I2CTransaction[] { st }, timeOut);

            faces = new byte[size][];
            faces[0] = new byte[] { 0x00, 0x00, 0x00, 0x18, 0x18, 0x00, 0x00, 0x00};
            faces[1] = new byte[] { 0x00, 0x00, 0x3C, 0x3C, 0x3C, 0x3C, 0x00, 0x00 };
            faces[2] = new byte[] { 0x00, 0x7E, 0x7E, 0x7E, 0x7E, 0x7E, 0x7E, 0x00 };
            faces[3] = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

            clearScreen();

            PWM trig = new PWM(Cpu.PWMChannel.PWM_0,60000,10, PWM.ScaleFactor.Microseconds, false);
            sound = new PWM(Cpu.PWMChannel.PWM_1, 100.0 , 0.5, false);
            InterruptPort d1H = new InterruptPort(Pins.GPIO_PIN_D0, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeBoth);
            d1H.OnInterrupt += new NativeEventHandler(d1H_OnInterrupt);

            trig.Start();
            sound.Start();
            Thread.Sleep(Timeout.Infinite);
            trig.Stop();
        }
        
        static void d1H_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            if(data2 == 1)
                startTime = DateTime.Now;
            else if (data2 == 0)
            {
                int j = 0;
                TimeSpan timeLapse = DateTime.Now - startTime;

                double dist = 350.0 * timeLapse.Milliseconds / 20;

                if (dist < 10)
                {
                    j = 3;
                    sound.Frequency = 1000;
                }
                else if (dist < 20)
                {
                    j = 2;
                    sound.Frequency = 500;
                }
                else if (dist < 50)
                {
                    j = 1;
                    sound.Frequency = 200;
                }
                else
                {
                    j = 0;
                    sound.Frequency = 100;
                }

                byte[] data = new byte[bufferSize];
                data[0] = 0x00; // Going to starting address

                for (uint i = 1; i < bufferSize; i += 2)
                {
                    data[i] = RotateRight(faces[j][(i - 1) / 2]);
                    data[i + 1] = 0x00;
                }

                st = I2CDevice.CreateWriteTransaction(data);
                matrix8x8.Execute(new I2CDevice.I2CTransaction[] { st }, timeOut); 
            }
        }

        public static byte RotateRight(byte val, int cnt = 1)
        {
            return (byte)((val >> cnt) | (val << (8 - cnt)));
        }

        public static void clearScreen()
        {
            byte[] data = new byte[bufferSize];
            data[0] = 0x00; // Going to starting address

            for (uint i = 1; i < bufferSize; i += 2)
            {
                data[i] = 0x00;
                data[i + 1] = 0x00;
            }
            st = I2CDevice.CreateWriteTransaction(data);
            matrix8x8.Execute(new I2CDevice.I2CTransaction[] { st }, timeOut); 
        }
    }
}
