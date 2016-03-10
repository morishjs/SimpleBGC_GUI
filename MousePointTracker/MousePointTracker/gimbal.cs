using System;
using System.Threading;
using System.Text;
using System.IO.Ports;
using System.Timers;


namespace gimbal
{

    //Writer : Junsuk Park
    //Class : SerialProtocol
    //Function : Connect with the gimbal and send command message.

    public class SerialProtocol
    {
        //Command Parameters.
        protected static byte CMD_READ_PARAMS = Convert.ToByte('R');
        protected static byte CMD_WRITE_PARAMS = Convert.ToByte('W');
        protected static byte CMD_REALTIME_DATA = Convert.ToByte('D');
        protected static byte CMD_BOARD_INFO = Convert.ToByte('V');
        protected static byte CMD_CALIB_ACC = Convert.ToByte('A');
        protected static byte CMD_CALIB_GYRO = Convert.ToByte('g');
        protected static byte CMD_CALIB_EXT_GAIN = Convert.ToByte('G');
        protected static byte CMD_USE_DEFAULTS = Convert.ToByte('F');
        protected static byte CMD_CALIB_POLES = Convert.ToByte('P');
        protected static byte CMD_RESET = Convert.ToByte('r');
        protected static byte CMD_HELPER_DATA = Convert.ToByte('H');
        protected static byte CMD_CALIB_OFFSET = Convert.ToByte('O');
        protected static byte CMD_CALIB_BAT = Convert.ToByte('B');
        protected static byte CMD_MOTORS_ON = Convert.ToByte('M');
        protected static byte CMD_MOTORS_OFF = Convert.ToByte('m');
        protected static byte CMD_CONTROL = Convert.ToByte('C');
        protected static byte CMD_TRIGGER_PIN = Convert.ToByte('T');
        protected static byte CMD_EXECUTE_MENU = Convert.ToByte('E');
        protected static byte CMD_GET_ANGLES = Convert.ToByte('I');
        protected static byte CMD_CONFIRM = Convert.ToByte('C');

        protected static byte CMD_BOARD_INFO_3 = 20;
        protected static byte CMD_READ_PARAMS_3 = 21;
        protected static byte CMD_WRITE_PARAMS_3 = 22;
        protected static byte CMD_REALTIME_DATA_3 = 23;
        protected static byte CMD_SELECT_IMU_3 = 24;
        protected static byte CMD_ERROR = (byte)255;
        protected static byte MAGIC_BYTE = Convert.ToByte('>');
        protected static bool BOARD_VERSION_3 = true;
        protected static float ANGLE_TO_DEGREE = 0.02197266F;

        public static int MODE_NO_CONTROL = 0;
        public static int MODE_SPEED = 1;
        public static int MODE_ANGLE = 2;
        public static int MODE_SPEED_ANGLE = 3;
        public static int MODE_RC = 4;

        // Fixed data[] positions, Plz check the protocol specification.
        protected static int MAGIC_BYTE_POS = 0;
        protected static int COMMAND_ID_POS = 1;
        protected static int DATA_SIZE_POS = 2;
        protected static int HEADER_CHECKSUM_POS = 3;
        protected static int BODY_DATA_POS = 4;
        protected static int readPosition = BODY_DATA_POS;

        protected static int herz = 50;
        protected static int count = 1;
        
        //Data structure when read real time values of the gimbal.
        protected static RealtimeDataStructure rtD = new RealtimeDataStructure();

        //"COM4" is variable, It depends on computers. Junsuk Com is 'COM4'
        //Baudrate is fixed at 115200. Other baudrate makes the gimbal work inproperly
        private static SerialPort port = new SerialPort("COM4", 115200);
        private byte[] byteArray = { 62, 67, 13, 80, 2, 85, 5, 85, 5, 85, 5, 85, 5, 85, 5, 85, 5, 30 };
        private static System.Timers.Timer timer = null;

        //Gimbal speed.
        private const int sPitch = 30;
        private const int sRoll = 30;
        private const int sYaw = 30;

        //Don't care about this. just for debugging 
        //public class readWorker
        //{   
        //    public readWorker()
        //    {
               
        //    }

        //    // This method will be called when the thread is started.
        //    public void DoWork()
        //    {
        //        while (!_shouldStop)
        //        {

        //            //일정시간동안 getAngle()을 호출한다. 
        //            Thread.Sleep(30); //1 per 30ms
        //            SerialProtocol.getAngle();
        //        }
        //        Console.WriteLine("worker thread: terminating gracefully.");
        //        //timer.Stop();
        //    }
        //    public void RequestStop()
        //    {
        //        _shouldStop = true;
                
        //    }
            
        //    // member will be accessed by multiple threads.
        //    private volatile bool _shouldStop = false;

        //}


        //Writer : Junsuk Park
        //Function : setAngle()
        //Set the speed and angle and send the command to the gimbal.
        //desired angle and speed values are decorated by 'ControlCommandStructure' 
                
        public static void setAngle(int roll, int pitch, int yaw, ref ControlCommandStructure cCmd)
        {

            byte[] byteRead;
            RealtimeDataStructure rData = rtD;
            cCmd.setMode(MODE_ANGLE);
            cCmd.setAnglePitch(pitch);
            cCmd.setAngleRoll(roll);
            cCmd.setAngleYaw(yaw);

            //setting the speed. Roll, Pitch, Yaw
            cCmd.setSpeedPitch(sPitch);
            cCmd.setSpeedRoll(sRoll);
            cCmd.setSpeedYaw(sYaw);
            
            if (sendCommand(CMD_CONTROL, cCmd.getControlStructure()))
            {

                //// 호출을 한번 더 해줘야 함.이유: 나중에 각도 읽어올때 쓰레기값 보냄
                //if (sendCommand(CMD_REALTIME_DATA))
                //{
                //    Thread.Sleep(20);
                //    if (port.BytesToRead >= 0)
                //    {
                //        byteRead = new byte[port.ReadBufferSize + 1];
                //        int count = port.Read(byteRead, 0, port.ReadBufferSize);
                //        rData = parseRealTimeData(byteRead);
                //    }
                //}
            }
            else
            {
                Console.WriteLine("Can't send command a message");
                return;
            }

        }


        //Writer : Junsuk Park
        //Function : getAngle()
        //Read the realtime values from the gimbal. Data Structure is 'RealtimeDataStructure'
        //If you want to get the angle from the gimbal, Just call rData.getAngle(). Other values is also available. Plz check the class functions.
        //
        public static float[] getAngle()
        {

            if(timer != null) timer.Enabled = false;
            byte[] byteRead;
            string[] lines = new string[3];
            RealtimeDataStructure rData = rtD;


            if (sendCommand(CMD_REALTIME_DATA))
            {
                Thread.Sleep(50);
                if(port.BytesToRead >= 0)
                {
                    byteRead = new byte[port.ReadBufferSize + 1];
                    int count = port.Read(byteRead, 0, port.ReadBufferSize);
                    rData = parseRealTimeData(byteRead);                    
                }
                          

                float[] angle = rData.getAngle();
                float[] frameAngle = rData.getRc_angle();

                //Console.WriteLine("받을 때 "+DateTime.Now + ":" + DateTime.Now.Millisecond);
                //Console.WriteLine("Roll: " + rData.getRoll());
                //Console.WriteLine("Pitch: " + rData.getPitch());
                //Console.WriteLine("Yaw: " + rData.getYaw());
                
                //Console.WriteLine("");

                lines[0] = rData.getRoll().ToString();
                lines[1] = rData.getPitch().ToString();
                lines[2] = rData.getYaw().ToString();
//              String Roll = "("+(1000/herz)*count+","+lines[0]+")"+",";
//              String Pitch = "(" + (1000 / herz) * count + "," + lines[1] + ")" + ",";
//              String Yaw = "(" + (1000 / herz) * count + "," + lines[2] + ")" + ",";
                String RPY = ((1000/ herz)* count).ToString() + "\t" + lines[0].ToString() + "\t" + lines[1].ToString() + "\t" + lines[2].ToString() + "\r\n"; 
                count++;
                System.IO.File.AppendAllText(@"C:\Users\Junsuk_Park\Roll.txt", RPY, Encoding.Default);
                //               System.IO.File.AppendAllText(@"C:\Users\Junsuk_Park\Roll.txt", Roll, Encoding.Default);
                //               System.IO.File.AppendAllText(@"C:\Users\Junsuk_Park\Pitch.txt", Pitch, Encoding.Default);
                //               System.IO.File.AppendAllText(@"C:\Users\Junsuk_Park\Yaw.txt", Yaw, Encoding.Default);
                

                if (timer != null)  timer.Enabled = true;
                return angle;
            }

            else return new float[0];

        }




        public static void timer_Elasped(object sender, ElapsedEventArgs e)
        {
            try
            {              
                
                SerialProtocol.getAngle();
                
            }
            finally
            {
                
            }      
   
        }

      //Just for debugging code, console mode.
      /*  static void Main(string[] args)
        {
            SerialProtocol p = new SerialProtocol();
            ControlCommandStructure cCmd = new ControlCommandStructure();
            ControlCommandStructure cCmd2 = new ControlCommandStructure();
            RealtimeDataStructure rData = rtD;
            String select;
            byte[] byteRead = new byte[100];
            int roll, pitch, yaw;
            int roll2, pitch2, yaw2;

            while (true)
            {
                
                Console.WriteLine("Input what you want");
                Console.WriteLine("1. Check the gimbal angle");
                Console.WriteLine("2. Set the gimbal angle");
                Console.WriteLine("3. Special mode");
                Console.WriteLine("4. Multi command");
                switch (select = Console.ReadLine())
                {
                    case "1":
                        getAngle();
                        break;

                    case "2":
                        Console.WriteLine("Give the angle value.");
                        roll = Convert.ToInt32(Console.ReadLine());
                        pitch = Convert.ToInt32(Console.ReadLine());
                        yaw = Convert.ToInt32(Console.ReadLine());
                        setAngle(roll, pitch, yaw, ref cCmd);
                        break;

                    case "3":
                        count = 1;
                        Console.WriteLine("###########Special mode##############");
                        Console.WriteLine("Give the angle value.");
                        roll = Convert.ToInt32(Console.ReadLine());
                        pitch = Convert.ToInt32(Console.ReadLine());
                        yaw = Convert.ToInt32(Console.ReadLine());
                        setAngle(roll, pitch, yaw, ref cCmd);


                        //Single Thread

                        //timer = new System.Timers.Timer(20);
                        //timer.Elapsed += new ElapsedEventHandler(timer_Elasped);
                        //timer.Enabled = true;                        
                        //timer.Start();
                        //Thread.Sleep(3000);
                        //timer.Stop();
                        //timer.Close();


                        readWorker workerObject = new readWorker();
                        Thread workerThread = new Thread(workerObject.DoWork);

                        

                        //Multi Threads

                        // Start the worker thread.
                        workerThread.Start();
                        // Loop until worker thread activates.
                        while (!workerThread.IsAlive) ;

                        // Put the main thread to sleep for 1 millisecond to
                        // allow the worker thread to do some work:
                        Thread.Sleep(2000);

                        // Request that the worker thread stop itself:
                        workerObject.RequestStop();

                        // Use the Join method to block the current thread 
                        // until the object's thread terminates.
                        workerThread.Join();
                        Console.WriteLine("main thread: Worker thread has terminated.");
                        break;

                    case "4":
                        Console.WriteLine("Multi command");

                        Console.WriteLine("Give the angle value.");
                        roll = Convert.ToInt32(Console.ReadLine());
                        pitch = Convert.ToInt32(Console.ReadLine());
                        yaw = Convert.ToInt32(Console.ReadLine());

                        Console.WriteLine("Give the angle value.");
                        roll2 = Convert.ToInt32(Console.ReadLine());
                        pitch2 = Convert.ToInt32(Console.ReadLine());
                        yaw2 = Convert.ToInt32(Console.ReadLine());

                        setAngle(roll, pitch, yaw, ref cCmd);
                        Thread.Sleep(100);
                        setAngle(roll2, pitch2, yaw2, ref cCmd2);

                        break;
                        aaa
                        
                }
            }




        }*/


        
        /*      
         * Reads the next word in the data array
         * 
         * @param data
         *            complete data array [header+body]
         * @return read bytes or -1 on failure
         */

        protected static int readWord(byte[] data)
        {
            if (data.Length >= readPosition + 2)
            {
                if (data[(readPosition)] > 200)
                {
                    data[(readPosition)] = 0;
                    if (data[(readPosition + 1)] > 200)
                        data[(readPosition + 1)] = 0;
                }
                return (data[(readPosition++)] & 0xFF)
                        + (data[(readPosition++)] << 8);
            }
            return -1;
        }

        /**
         * Reads the next unsigned word in the data array
         * 
         * @param data
         *            complete data array [header+body]
         * @return read bytes or -1 on failure
         */
        protected static int readWordUnsigned(byte[] data)
        {
            if (data.Length >= readPosition + 2)
            {
                return (data[(readPosition++)] & 0xFF)
                        + ((data[(readPosition++)] & 0xFF) << 8);
            }
            return -1;

        }

        /**
         * Reads the next byte in the data array
         * 
         * @param data
         *            complete data array [header+body]
         * @return read byte or -1 on failure
         */
        protected static int readByte(byte[] data)
        {
            if (readPosition < data.Length)
            {
                return data[(readPosition++)] & 0xFF;
            }
            return -1;

        }

        /**
         * Reads the next signed byte in the data array
         * 
         * @param data
         *            complete data array [header+body]
         * @return read byte or -1 on failure
         * @throws IOException
         */
        protected int readByteSigned(byte[] data)
        {

            if (readPosition < data.Length)
            {
                return data[(readPosition++)];
            }

            return -1;

        }

        protected bool readBoolean(byte[] data)
        {
            return readByte(data) == 1;
        }

        /**
         * Returns a (readable) String representation of the byte array
         * 
         * @param data
         *            complete data array [header+body]
         * @return bytes as a String
         */
        static String byteArrayToString(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in data)
            {

                sb.Append(Convert.ToString(b));
            }
            return sb.ToString();
        }
        

        public byte[] getByteArray()
        {
            return byteArray;
        }



        public SerialProtocol()
        {
            // Begin communications
            port.Open();
        }

        protected static bool verifyChecksum(byte[] data)
        {
            if (data.Length <= 4)
                return false;

            bool headerOK = false;
            bool bodyOK = false;

            if (data[MAGIC_BYTE_POS] == MAGIC_BYTE
                    && ((int)(0xff & data[COMMAND_ID_POS]) + (int)(0xff & data[DATA_SIZE_POS])) % 256 == (0xff & data[HEADER_CHECKSUM_POS]))
            {
                headerOK = true;
            }
            else {
                Console.WriteLine("Bad Header");
                return false;
            }

            int bodyChksm = 0;
            for (int i = 4; i < data.Length - 1; i++)
            {
                bodyChksm += (0xff & data[i]);
            }

            if ((bodyChksm % 256) == (0xff & data[data.Length - 1]))
            {
                bodyOK = true;
            }
            else {
                Console.WriteLine("Bad Body");
                return false;
            }

            return (headerOK && bodyOK);
        }

        public static bool sendCommand(byte commandID, byte[] rawData)
        {
            byte bodyDataSize = (byte)rawData.Length;
            byte headerChecksum = (byte)(((int)commandID + (int)bodyDataSize) % 256);
            int rawBodyChecksum = 0;
            int cnt = 0;
            do
            {
                if (cnt >= bodyDataSize)
                {
                    byte bodyChecksum = (byte)(rawBodyChecksum % 256);
                    byte[] headerArray = new byte[4];
                    headerArray[MAGIC_BYTE_POS] = MAGIC_BYTE;
                    headerArray[COMMAND_ID_POS] = (byte)(commandID & 0xff);
                    headerArray[DATA_SIZE_POS] = (byte)(bodyDataSize & 0xff);
                    headerArray[HEADER_CHECKSUM_POS] = (byte)(headerChecksum & 0xff);

                    byte[] headerAndBodyArray = new byte[1 + (headerArray.Length + rawData.Length)];
                    Array.Copy(headerArray, headerAndBodyArray, headerArray.Length);
                    Array.Copy(rawData, 0, headerAndBodyArray, headerArray.Length, rawData.Length);

                    headerAndBodyArray[headerArray.Length + rawData.Length] = (byte)(bodyChecksum & 0xff);

                    if (verifyChecksum(headerAndBodyArray))
                    {
          
                        port.Write(headerAndBodyArray, 0, headerAndBodyArray.Length);
                        return true;
                    }
                    else return false;


                }
                rawBodyChecksum += rawData[cnt];
                cnt++;
            } while (true);
        }

        //Basic wrapper function without bodydata
        public static bool sendCommand(byte commandID)
        {
            if (sendCommand(commandID, new byte[0]))
                return true;
            else return false;
        }

        public static RealtimeDataStructure getRealtimeDataStructure()
        {
            return rtD;
        }

        //Read data
        protected static RealtimeDataStructure parseRealTimeData(byte[] data)
        {

            for (int i = 0; i < 3; i++)
            {
                getRealtimeDataStructure().setAcc(readWord(data), i);
                getRealtimeDataStructure().setGyro(readWord(data), i);
            }

            for (int i = 0; i < getRealtimeDataStructure().getDebug().Length; i++)
            {
                getRealtimeDataStructure().setDebug(readWord(data), i);
            }
            for (int i = 0; i < getRealtimeDataStructure().getRcData().Length; i++)
            {
                getRealtimeDataStructure().setRcData(readWord(data), i);
            }
            if (BOARD_VERSION_3)
            {

                for (int i = 0; i < 3; i++)
                {

                    getRealtimeDataStructure().setAngle(
                            (readWord(data) * ANGLE_TO_DEGREE), i);

                }
                for (int i = 0; i < 3; i++)
                {
                    getRealtimeDataStructure().setFrameAngle(
                            (readWord(data) * ANGLE_TO_DEGREE), i);
                }
                for (int i = 0; i < 3; i++)
                    getRealtimeDataStructure().setRc_angle(
                            (readWord(data) * ANGLE_TO_DEGREE), i);
            }
            else {

                for (int i = 0; i < 3; i++)
                {
                    getRealtimeDataStructure().setAngle(
                            (readWord(data) * ANGLE_TO_DEGREE), i);
                }
                for (int i = 0; i < 3; i++)
                {
                    getRealtimeDataStructure().setRc_angle(
                            (readWord(data) * ANGLE_TO_DEGREE), i);
                }
            }

            getRealtimeDataStructure().setCycleTime(readWord(data));
            getRealtimeDataStructure().setI2cErrorCount(readWordUnsigned(data));
            getRealtimeDataStructure().setErrorCode(readByte(data));
            getRealtimeDataStructure().setBatteryValue(readWordUnsigned(data));
            getRealtimeDataStructure().setPowered(readByte(data) > 0);
            if (BOARD_VERSION_3)
            {
                getRealtimeDataStructure().setCurrentIMU(readByte(data));
            }
            getRealtimeDataStructure().setCurrentProfile(readByte(data));
            for (int i = 0; i < 3; i++)
            {
                getRealtimeDataStructure().setPower(readByte(data), i);
            }
            // Reset position to first Data-Byte
            readPosition = BODY_DATA_POS;
            return getRealtimeDataStructure();
        }
    }


    public class ControlCommandStructure
    {

        public static int MODE_NO_CONTROL = 0;
        public static int MODE_SPEED = 1;
        public static int MODE_ANGLE = 2;
        public static int MODE_SPEED_ANGLE = 3;
        public static int MODE_RC = 4;

        private static float ANGLE_TO_DEGREE = 0.02197266F;
        private static int mode = 0;
        private static int speedRoll = 0;
        private static int angleRoll = 0;
        private static int speedPitch = 0;
        private static int anglePitch = 0;
        private static int speedYaw = 0;
        private static int angleYaw = 0;
        private static byte[] controlData = new byte[13];

        public byte[] getControlStructure()
        {
            return getCmdControlDataArray();
        }

        private static byte getFirstByte(int i)
        {
            return (byte)(i & 0xff);
        }

        private static byte getSecondByte(int i)
        {
            return (byte)(0xff & i >> 8);
        }

        private static int Degree2Angle(int i)
        {
            int x = (int)(i * (1.0f / ANGLE_TO_DEGREE));

            return x;
        }

        public static int getIntSigned(byte byte0, byte byte1)
        {
            return (byte0 & 0xff) + (byte1 << 8);
        }

        public static byte[] getCmdControlDataArray()
        {
            controlData[0] = (byte)(0xff & mode);
            if (mode == MODE_ANGLE || mode == MODE_SPEED)
            {
                controlData[1] = getFirstByte(Degree2Angle(speedRoll));
                controlData[2] = getSecondByte(Degree2Angle(speedRoll));
                controlData[3] = getFirstByte(Degree2Angle(angleRoll));
                controlData[4] = getSecondByte(Degree2Angle(angleRoll));
                controlData[5] = getFirstByte(Degree2Angle(speedPitch));
                controlData[6] = getSecondByte(Degree2Angle(speedPitch));
                controlData[7] = getFirstByte(Degree2Angle(anglePitch));
                controlData[8] = getSecondByte(Degree2Angle(anglePitch));
                controlData[9] = getFirstByte(Degree2Angle(speedYaw));
                controlData[10] = getSecondByte(Degree2Angle(speedYaw));
                controlData[11] = getFirstByte(Degree2Angle(angleYaw));
                controlData[12] = getSecondByte(Degree2Angle(angleYaw));
            }
            else if (mode == MODE_RC)
            {

                controlData[1] = getFirstByte(speedRoll);
                controlData[2] = getSecondByte(speedRoll);
                controlData[3] = getFirstByte(angleRoll);
                controlData[4] = getSecondByte(angleRoll);
                controlData[5] = getFirstByte(speedPitch);
                controlData[6] = getSecondByte(speedPitch);
                controlData[7] = getFirstByte(anglePitch);
                controlData[8] = getSecondByte(anglePitch);
                controlData[9] = getFirstByte(speedYaw);
                controlData[10] = getSecondByte(speedYaw);
                controlData[11] = getFirstByte(angleYaw);
                controlData[12] = getSecondByte(angleYaw);

            }

            return controlData;
        }

        public static int getMode()
        {
            return mode;
        }

        public void setMode(int m)
        {
            mode = m;
        }

        public int getSpeedRoll()
        {
            return speedRoll;
        }

        public void setSpeedRoll(int s)
        {
            speedRoll = s;
        }

        public int getAngleRoll()
        {
            return angleRoll;
        }

        public void setAngleRoll(int a)
        {
            angleRoll = a;
        }

        public int getSpeedPitch()
        {
            return speedPitch;
        }

        public void setSpeedPitch(int s)
        {
            speedPitch = s;
        }

        public int getAnglePitch()
        {
            return anglePitch;
        }

        public void setAnglePitch(int a)
        {
            anglePitch = a;
        }

        public int getSpeedYaw()
        {
            return speedYaw;
        }

        public void setSpeedYaw(int s)
        {
            speedYaw = s;
        }

        public int getAngleYaw()
        {
            return angleYaw;
        }

        public void setAngleYaw(int a)
        {
            angleYaw = a;
        }
    }

    public class RealtimeDataStructure
    {

        private static RealtimeDataStructure realtimeData = new RealtimeDataStructure();
        private static int ROLL_CHANNEL = 0;
        private static int PITCH_CHANNEL = 1;
        private static int YAW_CHANNEL = 2;
        private static int RC_UNDEF = -8500;

        private int[] acc = new int[3];// {Roll, Pitch, Yaw}
        private int[] gyro = new int[3];// {Roll, Pitch, Yaw}
        private int[] debug = new int[4];
        private int[] rcData = { RC_UNDEF, RC_UNDEF, RC_UNDEF, RC_UNDEF, RC_UNDEF, RC_UNDEF };
        private float[] angle = new float[3]; // {Roll, Pitch, Yaw} - Actual angle
                                              // in degrees
        private float[] frameAngle = new float[3]; // {Roll, Pitch, Yaw}
        private float[] rc_angle = new float[3];// {Roll, Pitch, Yaw}
        private int cycleTime;
        private int i2cErrorCount;
        private int errorCode;
        private float batteryValue;
        private int currentProfile = 0;
        private int currentIMU = 1;
        private int[] power = new int[3];
        private bool isPowered;

        public static RealtimeDataStructure getRealtimeData()
        {
            return realtimeData;
        }

        public static void setCurrentRealtimeData(RealtimeDataStructure curRealtimeData)
        {
            realtimeData = curRealtimeData;
        }

        public int[] getAcc()
        {
            return acc;
        }

        public void setAcc(int acc, int position)
        {

            if (position < 3)
                this.acc[position] = acc;
        }

        public int[] getGyro()
        {
            return gyro;
        }

        public void setGyro(int gyro, int position)
        {
            this.gyro[position] = gyro;
        }

        public int[] getDebug()
        {
            return debug;
        }

        public void setDebug(int debug, int position)
        {
            if (position < 4)
                this.debug[position] = debug;
        }

        public int[] getRcData()
        {
            return rcData;
        }

        public void setRcData(int rc, int position)
        {
            if (position < 3)
                this.rcData[position] = rc;
        }

        public float[] getAngle()
        {
            return angle;
        }

        public float getRoll()
        {
            return angle[ROLL_CHANNEL];
        }

        public void setRoll(float f)
        {
            this.angle[ROLL_CHANNEL] = f;
        }

        public float getPitch()
        {
            return angle[PITCH_CHANNEL];
        }

        public void setPitch(float f)
        {
            this.angle[PITCH_CHANNEL] = f;
        }

        public float getYaw()
        {
            return angle[YAW_CHANNEL];
        }

        public void setYaw(float f)
        {
            this.angle[YAW_CHANNEL] = f;
        }

        public void setAngle(float angle, int position)
        {
            if (position < 3)
                this.angle[position] = angle;
        }

        public void setFrameAngle(float frameAngle, int position)
        {
            if (position < 3)
                this.frameAngle[position] = frameAngle;
        }

        public float[] getFrameAngle()
        {
            return frameAngle;
        }

        public float[] getRc_angle()
        {
            return rc_angle;
        }

        public void setRc_angle(float rc_angle, int position)
        {
            if (position < 3)
                this.rc_angle[position] = rc_angle;
        }

        public int getCycleTime()
        {
            return cycleTime;
        }

        public void setCycleTime(int cycleTime)
        {
            this.cycleTime = cycleTime;
        }

        public int getI2cErrorCount()
        {
            return i2cErrorCount;
        }

        public void setI2cErrorCount(int i2cErrorCount)
        {
            this.i2cErrorCount = i2cErrorCount;
        }

        public int getErrorCode()
        {
            return errorCode;
        }

        public void setErrorCode(int errorCode)
        {
            this.errorCode = errorCode;
        }

        public float getBatteryValue()
        {
            return batteryValue;
        }

        public void setBatteryValue(int val)
        {
            if (val > 0)
                this.batteryValue = val / 100;
        }

        public bool IsPowered()
        {
            return isPowered;
        }

        public void setPowered(bool isPowered)
        {
            this.isPowered = isPowered;
        }

        public int getCurrentProfile()
        {

            return currentProfile;
        }

        public void setCurrentProfile(int profile)
        {

            if (profile >= 0 && profile < 5)
            {

                this.currentProfile = profile;
            }
        }

        public int getCurrentIMU()
        {
            return currentIMU;
        }

        public void setCurrentIMU(int imu)
        {
            this.currentIMU = imu;
        }

        public int[] getPower()
        {
            return power;
        }

        public void setPower(int power, int position)
        {
            if (position < 3)
                this.power[position] = power;
        }

    }
}



