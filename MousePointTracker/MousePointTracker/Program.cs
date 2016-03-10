//**************************************************************
//#####################Application part########################
//App for the gimbal in accordance with mouse point position.
//#############################################################
//**************************************************************


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using gimbal;
using System.Runtime.InteropServices;
using System.Threading;

namespace MousePointTracker
{
    //Writer : Junsuk Park
    //Class : MousePointTracker
    //Function : Main class extends Windows.Forms which is for GUI implementation.

    public class MousePointTracker : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        

        private const int fontSize = 40;

        static SerialProtocol p = new SerialProtocol();
        static ControlCommandStructure cCmd = new ControlCommandStructure();
        static System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();

        //Ensure to send the command to gimbal 
        [DllImport("kernel32.dll")]
        extern static short QueryPerformanceCounter(ref long x);
        [DllImport("kernel32.dll")]
        extern static short QueryPerformanceFrequency(ref long x);

        static double herz = 0;
        static double elapsedTime;
        static long freq;

        private static int mouseX = 0, mouseY = 0;

        static long ctr = 0;
        static float[] angle;
           

        //Writer : Junsuk Park
        //Class : ReadWorker
        //Function : Read mouse position and send command to gimbal. (200Hz)
        public class ReadWorker
        {


            public ReadWorker()
            {

            }

            // This method will be called when the thread is started.
            public void DoWork()
            {
                while (!_shouldStop)
                {

                    //일정시간동안 setAngle()을 호출한다. 
                    // 1 per 5ms, 200Hz
                    if (p != null)
                    { //한 쓰레드에 걸리는 시간 - 한 쓰레드에서 Sleep 빼고 돌아가는 시간  = 한 쓰레드의 Sleep 시간 
                        long tmp = 0;
                        QueryPerformanceCounter(ref tmp);
                        SerialProtocol.setAngle(0, mouseY, mouseX, ref cCmd);

                        QueryPerformanceCounter(ref ctr);
                        herz = (double)(ctr - tmp) / (double)freq;
                    }
                    double elapsedTime = (1.0 / herz) * 1000;
                    if((int)elapsedTime < 5)
                        Thread.Sleep(5 - (int)elapsedTime); //1 per 5ms
                }
                Console.WriteLine("worker thread: terminating gracefully.");
             
            }
            public void RequestStop()
            {
                _shouldStop = true;

            }

            // member will be accessed by multiple threads.
            private volatile bool _shouldStop = false;
        }


        [STAThread]
        static void Main()
        {

            QueryPerformanceFrequency(ref freq);


            //ReadWorker class initialized
            ReadWorker workerObject = new ReadWorker();
            Thread workerThread = new Thread(workerObject.DoWork);
            workerThread.Start();


            Application.Run(new MousePointTracker());
        }


        //Writer : Junsuk Park
        //Function : MousePointTracker()
        //Just about Widget ( panel, label etc..)
        public MousePointTracker()
        {
            

            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();            
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();            

            // Mouse Events Label
            this.label1.Location = new System.Drawing.Point(24, 504);
            this.label1.Size = new System.Drawing.Size(392, 23);
            // DoubleClickSize Label
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(24, 48);
            this.label2.Size = new System.Drawing.Size(35, 13);
            // DoubleClickTime Label
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(24, 72);
            this.label3.Size = new System.Drawing.Size(35, 13);
            // MousePresent Label
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(24, 96);
            this.label4.Size = new System.Drawing.Size(35, 13);
            // MouseButtons Label
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(24, 120);
            this.label5.Size = new System.Drawing.Size(35, 13);
            // MouseButtonsSwapped Label
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(320, 48);
            this.label6.Size = new System.Drawing.Size(35, 13);
            // MouseWheelPresent Label
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(320, 72);
            this.label7.Size = new System.Drawing.Size(35, 13);
            // MouseWheelScrollLines Label
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(320, 96);
            this.label8.Size = new System.Drawing.Size(35, 13);
            // NativeMouseWheelSupport Label
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(320, 120);
            this.label9.Size = new System.Drawing.Size(35, 13);

            // Mouse Panel
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right);
            this.panel1.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Size = new System.Drawing.Size(500, 500);   
            this.panel1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseMove);
            this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            this.panel1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseDown);
            // Set up how the form should be displayed and add the controls to the form.
            this.ClientSize = new System.Drawing.Size(500, 500);
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                                        this.label9,this.label8,this.label7,this.label6,
                                        this.label5,this.label4,this.label3,this.label2,
                                        this.panel1,this.label1});
            this.Text = "Mouse Event Example";
            

        }

        //Writer : Junsuk Park
        //Function : panel1_MouseMove
        //Arguments: 
        //System.Windows.Forms.MouseEventArgs e : 'e' has the information when mouse event occurs
        //Show mouse position and herz in moving.
        private void panel1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {   
            mouseX = e.X/5 - 50;
            mouseY = e.Y/5 - 50;
           
            double time = herz * 1000;
            int h = (int)(1000 / time);
            this.label6.Text = h.ToString() + "Hz";
            
            this.label3.Text = "X: " + mouseX.ToString();
            this.label4.Text = "Y: " + mouseY.ToString();                        
        }

        //Writer : Junsuk Park
        //Function : panel1_MouseDown
        //Print the angle to display (Actually, the angle in reading from the gimbal has weird value. (Not solved..)
        private void panel1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // Update the mouse path with the mouse information
            Point mouseDownLocation = new Point(e.X, e.Y);
            angle = SerialProtocol.getAngle();
            string eventString = null;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (angle[1] > 360)
                    {
                        //angle[1] = angle[1] - 360;
                    }
                    this.label8.Text = "Pitch: " + angle[1].ToString();
                    if(angle[2] > 360)
                    {
                       // angle[2] = angle[2] - 360;
                    }
                    this.label7.Text = "Yaw: " + angle[2].ToString();
                    break;

                default:
                    break;
            }


            panel1.Focus();
            panel1.Invalidate();
        }


        //Writer : Junsuk Park
        //Function : panel1_Paint
        //Just draw x, y axis
        private void panel1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            Pen blackpen = new Pen(Color.Black, 3);
            Graphics g = e.Graphics;
            g.DrawLine(blackpen, 0, 250, 500, 250);
            g.DrawLine(blackpen, 250, 0, 250, 500);
        }

    }
}
