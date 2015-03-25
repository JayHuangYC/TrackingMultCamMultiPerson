using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using Emgu.Util;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections;

namespace PeopleDetection
{
    public partial class Main : Form
    {
        public static int numcam = 3;
        public static int ThSub = 20, ThBg = 50;
        public static double Dis = 0.47;

        public static int w = 320;
        public static int h = 240;
        public static int b = 16;


        public static Image<Bgr, Byte> modelImg = null;
        public static Image<Bgr, Byte>[] bgImage = new Image<Bgr,byte>[numcam];
        public static Image<Bgr, Byte>[] currImage = new Image<Bgr, byte>[numcam];
        public static Image<Bgr, Byte>[] prevImage = new Image<Bgr, byte>[numcam];

        public static Image<Gray, Byte>[] bgImgGy = new Image<Gray, byte>[numcam];
        public static Image<Gray, Byte>[] currImgGy = new Image<Gray, byte>[numcam];
        public static Image<Gray, Byte>[] prevImgGy = new Image<Gray, byte>[numcam];
        public static byte[, ,] bgImgGyPix = new byte[h, w, numcam];


        //public static Image<Bgr, Byte> bgImage2 = null;
        //public static Image<Bgr, Byte> currImage2 = null;
        //public static Image<Bgr, Byte> prevImage2 = null;

        //public static Image<Gray, Byte> bgImgGy2 = null;
        //public static Image<Gray, Byte> currImgGy2 = null;
        //public static Image<Gray, Byte> prevImgGy2 = null;
        //public static byte[, ,] bgImgGyPix2 = new byte[h, w, 1];

        public static String state1="idle";
        public static String state2 = "idle";
        public static String state3 = "idle";

        public static bool tracking = true;
        public static int lasttrack = -1;

        bool drawflag = false;
        int x1, y1, x2, y2;
       
        Rectangle selection, subrect;
        public static bool modelFlag = false;

        Image<Bgr, Byte> hue, backproject;
        Image<Hsv, Byte> hsv;
        Image<Gray, Byte> mask;
        public static DenseHistogram hist;
        int hdims = 16;
        float[] hranges_arr = { 0, 180 };
        RangeF hranges = new RangeF(0, 180);
       
        int smin = 30;//m_s_min30
        int vmin = 3;//m_b_min10
        int vmax = 256;//m_b_max
        int i, j;
        
        //public static int state = 1;
                
        Image<Gray, byte> finalBlobImg1 = null;
        ArrayList blobRect1 = null;

        //Image<Gray, byte> finalBlobImg2 = null;
       // ArrayList blobRect2 = null;
       
        //public static Rectangle subrect;

        public static Rectangle track_window_mean1,track_window_mean2;        
        MCvScalar s;

        //CvFont font;
        //cvInitFont( &font, CV_FONT_HERSHEY_PLAIN, 0.7, 0.7, 0, 1, CV_AA );

        MCvBox2D track_box = new MCvBox2D(new PointF(0, 0), new SizeF(1, 1), 0);

        public static Capture[] cap=new Capture[numcam];


        public Thread[] process = new Thread[numcam];
        CamProcess[] camera = new CamProcess[numcam];

        public Main()
        {
            InitializeComponent();
            textBox1.Text = ThBg.ToString();
            textBox2.Text = Dis.ToString();
            //### 1. Connect to camera or file

            //read a video and extract a frame from it            
            //cap = new Capture("C:\\D\\11.avi");            
            cap[0] = new Capture("c:\\d\\Samples\\cam41.avi");
            cap[1] = new Capture("c:\\d\\Samples\\cam42.avi");
            cap[2] = new Capture("c:\\d\\Samples\\cam43.avi");
  
        }

        private void ConnectBT_Click(object sender, EventArgs e)
        {
            //### 2. capture frames and display in the pictureBox1 for user input
            for (int i = 0; i < 50; i++)
            {
                currImage[0] = cap[0].QueryFrame();
                currImage[0] = currImage[0].Resize(pictureBox1.Width, pictureBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
                pictureBox1.Image = currImage[0].Bitmap;

                //currImage[1] = cap[1].QueryFrame();
                //currImage[1] = currImage[1].Resize(pictureBox2.Width, pictureBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
                //pictureBox2.Image = currImage[1].Bitmap;

                //currImage[2] = cap[2].QueryFrame();
                //currImage[2] = currImage[2].Resize(pictureBox3.Width, pictureBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
                //pictureBox3.Image = currImage[2].Bitmap;
                //Console.WriteLine("###Current Image updated...");
            }
        }

        private void ExitBT_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < numcam; i++)
            {
                
                if (process[i] != null)
                    process[i].Suspend();
            }
            
            Application.Exit();
        }

        private void ProcessingBT_Click(object sender, EventArgs e)
        {
          //  camera1 = new Process(cap1, bgImage1, pictureBox1, pictureBox2, pictureBox3, track_window_mean1);
          //  process1 = new Thread(new ThreadStart(camera1.run));
          //  process1.Name = "camera1";
          // // process1.Start();
          ////  Process.START = true;
          //  camera2 = new Process(cap2, bgImage2, pictureBox4, pictureBox5, pictureBox6, track_window_mean2);
          //  process2 = new Thread(new ThreadStart(camera2.run));
          //  process2.Name = "camera2";
          // // process2.Start();


            camera[0] = new CamProcess(cap[0], bgImage[0], pictureBox1, pictureBox4, null, track_window_mean1,"TRACK",0);
            process[0]= new Thread(new ThreadStart(camera[0].run));
            process[0].Name = "camera1";            
            process[0].Start();

            camera[1] = new CamProcess(cap[1], bgImage[1], pictureBox2, pictureBox5, null, track_window_mean1, "BGSUB", 1);
            process[1] = new Thread(new ThreadStart(camera[1].run));
            process[1].Name = "camera2";
            process[1].Start();

            camera[2] = new CamProcess(cap[2], bgImage[2], pictureBox3, pictureBox6, null, track_window_mean1, "BGSUB", 2);
            process[2] = new Thread(new ThreadStart(camera[2].run));
            process[2].Name = "camera3";
            process[2].Start();

            
            //Manager manager = new Manager(cam1, cam2, null);
            //Thread managerthread = new Thread(new ThreadStart(manager.run));
            //managerthread.Name = "manager";
            //managerthread.Start();


           // Process.START = true;

        }
        //convert Bitmap to 2D array        
        public byte[, ,] bitmap22D(Image<Bgr, Byte> image)
        {
            int w = image.Width;
            int h = image.Height;
            byte[, ,] pixels = new byte[h, w, 1];
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    pixels[i, j, 0] = image.Data[i, j, 0];
                    //Console.Write(pixels[i, j,0] + " ");
                }
                //Console.Write("\n");
            }
            return pixels;
        }
        public byte[, ,] bitmap22D(Image<Gray, Byte> image)
        {
            int w = image.Width;
            int h = image.Height;
            byte[, ,] pixels = new byte[h, w, 1];
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    pixels[i, j, 0] = image.Data[i, j, 0];
                    //Console.Write(pixels[i, j] + " ");
                }
                //Console.Write("\n");                
            }
            return pixels;
        }

     

        private void mouseup(object sender, MouseEventArgs e)
        {
            //MessageBox.Show("click");
            drawflag = false;
            x2 = e.X;
            y2 = e.Y;
            selection = new Rectangle(x1, y1, (x2 - x1), (y2 - y1));
            Image<Bgr,byte> currImgClone=currImage[0].Clone();
            currImgClone.Draw(new Rectangle(x1, y1, (x2 - x1), (y2 - y1)), new Bgr(255, 255, 255), 2);
            pictureBox1.Image = currImgClone.Bitmap;
            Process.START = true;
            Console.WriteLine("###Model Region seleted...");
        }

        private void mousemove(object sender, MouseEventArgs e)
        {
            if (drawflag)
            {
                x2 = e.X;
                y2 = e.Y;
                currImage[0].Draw(new LineSegment2D(new Point(x1, y2), new Point(x1, y2)), new Bgr(255, 255, 255), 2);
                currImage[0].Draw(new LineSegment2D(new Point(x2, y1), new Point(x2, y1)), new Bgr(255, 255, 255), 2);
                currImage[0].Draw(new LineSegment2D(new Point(x1, y2), new Point(x1, y2)), new Bgr(255, 255, 255), 2);
                //currImage[0].Draw(new LineSegment2D(new Point(x2, y2), new Point(x2, y2)), new Bgr(255, 255, 255), 2);
                pictureBox1.Image = currImage[0].Bitmap;

            }
        }

        private void mousedown(object sender, MouseEventArgs e)
        {

            x1 = e.X;
            y1 = e.Y;
            drawflag = true;
            Process.START = false;

        }

        private void BGCalc_Click(object sender, EventArgs e)
        {
            //### Captures the background image and stores in "bgImage"
            
            bgImage[0] = cap[0].QueryFrame();
            bgImage[0] = bgImage[0].Resize(pictureBox1.Width, pictureBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            bgImgGy[0] = bgImage[0].Convert<Gray, Byte>().Resize(w, h, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            pictureBox1.Image = bgImgGy[0].Bitmap;
            //bgImgGyPix[, ,0] = bitmap22D(bgImgGy[0]);
            Console.WriteLine("###Background Image updated...");

            bgImage[1] = cap[1].QueryFrame();
            bgImage[1] = bgImage[1].Resize(pictureBox2.Width, pictureBox2.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            bgImgGy[1] = bgImage[1].Convert<Gray, Byte>().Resize(w, h, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            pictureBox2.Image = bgImgGy[1].Bitmap;
            //bgImgGyPix[, ,1] = bitmap22D(bgImgGy[1]);

            bgImage[2] = cap[2].QueryFrame();
            bgImage[2] = bgImage[2].Resize(pictureBox3.Width, pictureBox2.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            bgImgGy[2] = bgImage[2].Convert<Gray, Byte>().Resize(w, h, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            pictureBox3.Image = bgImgGy[2].Bitmap;
            //bgImgGyPix[, ,2] = bitmap22D(bgImgGy[2]);

            //### Captures the background image and stores in "bgImage"
           
        }

        private void Model_Click(object sender, EventArgs e)
        {

            //### set currImage which has been used for taking model as modelImage
            modelImg = currImage[0];

            //### Background Subtraction ~ currImage is subtracted with the bgImage and the result is stored in finalBlobImg.
            bgSubtraction(currImage[0],bgImage[0],ref finalBlobImg1,ref blobRect1);            

            /* Allocate buffers */
            hsv = new Image<Hsv, Byte>(w, h);
            hue = new Image<Bgr, Byte>(w, h);
            mask = new Image<Gray, Byte>(w, h);
            backproject = new Image<Bgr, Byte>(w, h);
            hist = new DenseHistogram(hdims, hranges);//cvCreateHist(1, &hdims, CV_HIST_ARRAY, &hranges, 1);
            hsv = modelImg.Convert<Hsv, Byte>();

            //extract the hue and value channels
            Image<Gray, Byte>[] channels = hsv.Split();  //split into components
            Image<Gray, Byte>[] imghue = new Image<Gray, byte>[1]; imghue[0] = channels[0];            //hsv, so channels[0] is hue.
            Image<Gray, Byte> imgval = channels[2];            //hsv, so channels[2] is value.
            Image<Gray, Byte> imgsat = channels[1];            //hsv, so channels[1] is saturation.

            /**
	    	 * Check if the pixels in hsv fall within a particular range.
		     * H: 0 to 180
		    * S: smin to 256
		    * V: vmin to vmax
		    * Store the result in variable: mask
		    */
            Hsv hsv_lower = new Hsv(0, smin, Math.Min(vmin, vmax));
            Hsv hsv_upper = new Hsv(180, 256, Math.Max(vmin, vmax));
            mask = hsv.InRange(hsv_lower, hsv_upper);

            //setting ROI to images
            mask.ROI = selection;
            imghue[0]=imghue[0].And(finalBlobImg1.Erode(3));
            imghue[0].ROI = selection;

            /**
		    * Calculate the histogram of selected region
		    * Store the result in variable: hist
		    */
            hist.Calculate(imghue, false, mask);

            /* Scale the histogram */
            float hMin, hMax;
            int[] minLoc;
            int[] maxLoc;
            hist.MinMax(out hMin, out hMax, out minLoc, out maxLoc);

            /* Reset ROI for hue and mask */
            CvInvoke.cvResetImageROI(imghue[0]);
            CvInvoke.cvResetImageROI(mask);

            /* Set tracking windows */
            track_window_mean1 = selection;
            //track_window_mean2 = selection;

            //### Update the pictureBoxes with mask and hsv images

            //pictureBox2.Image = mask.Bitmap;
            //pictureBox2.Image = finalBlobImg1.Bitmap;
            //pictureBox4.Image = finalBlobImg1.Bitmap;
            //pictureBox3.Image = imghue[0].Or(finalBlobImg1).Bitmap;

            Console.WriteLine("###Model Captured....");
            modelFlag = true;
        }

        public void bgSubtraction(Image<Bgr, Byte> currImage, Image<Bgr, Byte> bgImage, ref Image<Gray, byte> finalBlobImg, ref ArrayList blobRect)
        {
            //### inputs are currImage and bgImage. Output is finalBlobImg - 1 for FG and 0 for BG and blobRect to store ROIs of each blob in this frame
            //### extracting pixels from currImage and bgImage
            byte[, ,] curImgPix = new byte[Main.w, Main.h, 1];
            byte[, ,] bw_2d = new byte[Main.h / Main.b, Main.w / Main.b, 1];
            curImgPix = bitmap22D(currImage);
            Image<Gray, byte> currImgGy = currImage.Convert<Gray, byte>();
            Image<Gray, byte> bgImgGy = bgImage.Convert<Gray, byte>();

            //### Motion Detection between currImage pixels and bgImage pixels
            motionDetection(curImgPix, bitmap22D(bgImgGy), Main.w, Main.h, Main.b, ref bw_2d);

            //Component Labeling for localizing objects that are moving. There can be more than one objects that are moving in the scene.
            byte m = 1;
            ArrayList mv = new ArrayList();
            for (int x = 0; x < h / b; x++)
            {
                for (int y = 0; y < w / b; y++)
                {
                    if (bw_2d[x, y, 0] == 1)
                    {
                        ArrayList label = new ArrayList();
                        compLabel(x, y, ++m, label, ref bw_2d);
                        mv.Add(label);
                    }
                }
            }
            //Console.WriteLine("-----No of Blobs=" + mv.Count);

            //### outimage - binay image of each blob; finalBlobImg - OR of all blob images to get final binay image with 1 for FG and 0 for BG
            finalBlobImg = new Image<Gray, byte>(w, h);
            Image<Gray, byte> outimage = new Image<Gray, byte>(w, h);
            //### blobRect - to store the ROIs of each blob in the currImage.
            blobRect = new ArrayList();

            //Calculate xmin ymin xmax ymax for each blob detected
            foreach (ArrayList blob in mv)
            {
                if (blob.Count > 10)   //no of blocks in each blob > 3
                {
                    //Console.WriteLine(blob.Count+"\n");
                    int xmin, xmax, ymin, ymax;

                    IEnumerator iblob = blob.GetEnumerator();
                    iblob.MoveNext();
                    ArrayList pt = (ArrayList)iblob.Current;
                    IEnumerator ipt = pt.GetEnumerator();
                    ipt.MoveNext();
                    //Console.Write("{y"+ipt.Current);
                    ymin = ymax = (int)ipt.Current;
                    ipt.MoveNext();
                    xmin = xmax = (int)ipt.Current;
                    //Console.Write(" x"+ipt.Current+"}\n");


                    while (iblob.MoveNext())
                    {
                        ArrayList ptt = (ArrayList)iblob.Current;
                        IEnumerator iptt = ptt.GetEnumerator();
                        iptt.MoveNext();

                        int y = (int)iptt.Current;
                        iptt.MoveNext();
                        int x = (int)iptt.Current;
                        // Console.Write(x+","+y+" ; ");

                        if (xmin > x)
                            xmin = x;
                        if (xmax < x)
                            xmax = x;
                        if (ymin > y)
                            ymin = y;
                        if (ymax < y)
                            ymax = y;

                    }
                    //Console.WriteLine("****" + xmin + " " + xmax + " " + ymin + " " + ymax+" "+blob.Count);
                    // g.drawRect((xmin*blk),(ymin*blk),((xmax-xmin)*blk)+blk,((ymax-ymin)*blk)+blk);

                    subrect = new Rectangle((xmin * b), (ymin * b), ((xmax - xmin) * b) + b, ((ymax - ymin) * b) + b);
                    blobRect.Add(subrect);
                    //currImage.Draw(subrect, new Bgr(0, 0, 0), 2);

                    Image<Gray, byte> subImgBg = bgImgGy.GetSubRect(subrect);
                    Image<Gray, byte> subImgFg = currImgGy.GetSubRect(subrect);

                    subImgBg.Save("bg.jpg");
                    subImgFg.Save("fg.jpg");

                    Image<Gray, byte> imMask = subImgFg.AbsDiff(subImgBg);

                    for (int i = 0; i < subrect.Height; i++)
                    {
                        for (int j = 0; j < subrect.Width; j++)
                        {

                            //subImgFg.Data[i, j, 0] = 255;
                            if (imMask.Data[i, j, 0] < ThSub)
                            {
                                imMask.Data[i, j, 0] = 0;
                                outimage.Data[i + subrect.Y, j + subrect.X, 0] = 0;
                            }
                            else
                            {
                                imMask.Data[i, j, 0] = 255;
                                outimage.Data[i + subrect.Y, j + subrect.X, 0] = 255;
                            }
                            //Console.Write(subImgFg.Data[i, j, 0] + " ");

                        }
                        //Console.WriteLine();
                    }

                    //imMask._Erode(1);
                    //imMask._Dilate(2);
                }
                outimage._Erode(1);
                outimage._Dilate(2);
                finalBlobImg = finalBlobImg.Or(outimage);
            }           
        }

        public void motionDetection(byte[, ,] one_2d, byte[, ,] two_2d, int w, int h, int b, ref byte[, ,] bw_2d)
        {

            for (int r = 0; r < (h - b + 1); r = r + b)            //(h-b+1);r=r+blk)
            {
                for (int c = 0; c < (w - b + 1); c = c + b)             //(w-b+1);c=c+blk)
                {
                    //move to each blocks in the current frame
                    if (subtraction(one_2d, r, c, two_2d, r, c, b) > ThBg)
                    {
                        //Binary Array for each block in the image,
                        //such that array value is 1 if the block has 
                        //huge change, else array value is 0.
                        bw_2d[r / b, c / b, 0] = 1;
                        //Console.Write("1 ");
                    }
                    else
                    {
                        bw_2d[r / b, c / b, 0] = 0;
                        //Console.Write("0 ");
                    }

                }
                //Console.WriteLine();
            }
        }
        
        void compLabel(int i, int j, byte m, ArrayList xy, ref byte[, ,] bw_2d)
        {

            if (i >= 0 && i < h / b && j >= 0 && j < w / b)
            {
                ArrayList points = new ArrayList();
                if (bw_2d[i, j, 0] == 1)
                {
                    bw_2d[i, j, 0] = m;
                    points.Add(i);
                    points.Add(j);
                    xy.Add(points);
                    compLabel(i - 1, j - 1, m, xy,ref bw_2d);
                    compLabel(i - 1, j, m, xy, ref bw_2d);
                    compLabel(i - 1, j + 1, m, xy, ref bw_2d);
                    compLabel(i, j - 1, m, xy, ref bw_2d);
                    compLabel(i, j + 1, m, xy, ref bw_2d);
                    compLabel(i + 1, j - 1, m, xy, ref bw_2d);
                    compLabel(i + 1, j, m, xy, ref bw_2d);
                    compLabel(i + 1, j + 1, m, xy, ref bw_2d);

                }
            }
        }
        public int subtraction(byte[, ,] first, int x, int y, byte[, ,] second, int r, int c, int b)
        {
            //calculates average sum of diff between two blocks
            int sum = 0;
            for (int row = 0; row < b; row++)
            {
                for (int col = 0; col < b; col++)
                {
                    sum = sum + Math.Abs(first[x + row, y + col, 0] - second[r + row, c + col, 0]);
                }
            }

            return (sum / (b * b));
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar==13)
            {
                Main.ThBg = Int16.Parse(textBox1.Text);
                Console.WriteLine(Main.ThBg);
                //MessageBox.Show("test");
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                Main.Dis = Double.Parse(textBox2.Text);
                Console.WriteLine(Main.Dis);
                //MessageBox.Show("test");
            }
        }

     

     
    }
}
