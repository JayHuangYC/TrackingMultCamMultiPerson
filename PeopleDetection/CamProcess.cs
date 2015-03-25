using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;

using System.IO;

using Emgu.Util;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Windows.Forms;
using System.Collections;

namespace PeopleDetection
{
    public struct Blob
    {
        public Rectangle rect;
        public double distance;      

    }

    public class CamProcess
    {
        public ArrayList blobDistanceList=new ArrayList(1);
        public int camno = -1;
        int h = Main.h;
        int w = Main.w;
        int b = Main.b;
        public Gray cnt =new Gray(255);
        int k = 0;

        int hdims = 16;
        float[] hranges_arr = { 0, 180 };
        RangeF hranges = new RangeF(0, 180);

        int smin = 30;//m_s_min30
        int vmin = 3;//m_b_min10
        int vmax = 256;//m_b_max
        public static bool START = false;
        public String state = "";

        Capture cap = null;
        PictureBox pictureBox1, pictureBox2, pictureBox3 = null;
        Image<Bgr, Byte> currImage = null;
        Image<Gray, byte> currImgGy = null;
        Image<Bgr, Byte> bgImage = null;
        Image<Gray, byte> bgImgGy = null;
  
        byte[, ,] bgImgPix = new byte[Main.w, Main.h, 1];

        byte[, ,] curImgPix = new byte[Main.w, Main.h, 1];
        byte[, ,] bw_2d = new byte[Main.h / Main.b, Main.w / Main.b, 1];
        
        Image<Gray, Byte> mask = null;
        Image<Gray, byte> finalBlobImg = null;
        Image<Gray, byte> outimage = null;

        ArrayList blobRect = null;
        public Rectangle subrect, track_window_mean;
        enum P_State{BGSUB,TRACK,IDLE};


        public CamProcess(Capture cap, Image<Bgr, Byte> bgImage, PictureBox pictureBox1, PictureBox pictureBox2, PictureBox pictureBox3, Rectangle track_window_mean, String state, int camno)
        {
            this.cap = cap;
            this.pictureBox1 = pictureBox1;
            this.pictureBox2 = pictureBox2;
            this.pictureBox3 = pictureBox3;
            this.bgImage = bgImage;
            this.state = state;
            this.track_window_mean = track_window_mean;
            this.camno = camno;

            bgImgGy = bgImage.Convert<Gray, byte>();
            bgImgPix = bitmap22D(bgImage);
        }

        public void run()
        {
           if (Thread.CurrentThread.Name.EndsWith("2"))
               Thread.Sleep(10000);//MessageBox.Show("play " + Thread.CurrentThread.Name);// // 
            if (Thread.CurrentThread.Name.EndsWith("3"))
                Thread.Sleep(23000); //MessageBox.Show("play " + Thread.CurrentThread.Name);// Thread.Sleep(5000);
            try
            {
                //if (Thread.CurrentThread.Name.Equals("camera2"))
                //    Thread.Sleep(1000);
                switch (camno)
                {
                    case 1:
                        break;
                    case 2:
                        break;
                    case 3:
                        break;
                    default:
                        break;
                }

                //Thread Processing
                while ((currImage = cap.QueryFrame()) != null)
                {
                    //### updating the currImage every time                
                    currImage = currImage.Resize(pictureBox1.Width, pictureBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
                    //### Background Subtraction ~ currImage is subtracted with the bgImage and the result is stored in finalBlobImg.
                    
                   
                    switch (this.state)
                    {
                        case "BGSUB":
                                                        
                           // Console.WriteLine("_________________");

                            if (!Main.tracking)//&& Main.lasttrack!=camno
                            {
                                Console.WriteLine("###########" + Thread.CurrentThread.Name + "starts BGSUB");
                                blobDistanceList = bgSubtraction(currImage, bgImage, ref finalBlobImg, ref blobRect);
                               
                                if (blobDistanceList != null)
                                {
                                    foreach (Blob blob in blobDistanceList)
                                    {
                                        //Console.WriteLine("Blob::" + blob.distance + " " + blob.rect);
                                        if (blob.distance < Main.Dis) //set the tracker to cam with same object //&& Main.lasttrack!=camno
                                        {
                                            track_window_mean = blob.rect;
                                            if (blob.rect.Width < 50)
                                                track_window_mean.Width = 60;
                                            else                                             
                                                track_window_mean.Width = blob.rect.Width;

                                            if (blob.rect.Width + blob.rect.X > 319)
                                            {
                                                track_window_mean.X = track_window_mean.X - 10;
                                                track_window_mean.Width = 40;
                                            }
                                            if (blob.rect.Height + blob.rect.Y > 239)
                                                track_window_mean.Height = track_window_mean.Height - 20;
                                            if (blob.rect.Y == 0)
                                                track_window_mean.Y = blob.rect.Y + 15;

                                            //Console.WriteLine("Blob::::::::" + blob.distance + " " + blob.rect);
                                            Main.lasttrack = camno;
                                            Main.tracking = true;
                                            state = "TRACK";
                                            Console.WriteLine("###########" + Thread.CurrentThread.Name + "starts TRACK");
                                            break;
                                        }
                                    }
                                }

                            }
                          
                            break;

                        case "TRACK":

                            blobDistanceList = bgSubtraction(currImage, bgImage, ref finalBlobImg, ref blobRect);
                            //--------------
                            foreach (Blob blob in blobDistanceList)
                            {
                                Console.WriteLine("Tracking Blob== " + blob.distance + " " + blob.rect);                               
                            }
                            Console.WriteLine("----");
                            //-------------

                            Image<Hsv, Byte> hsv = new Image<Hsv, Byte>(w, h);
                            hsv = currImage.Convert<Hsv, Byte>();
                            Console.WriteLine("1");
                            //extract the hue and value channels
                            Image<Gray, Byte>[] channels = hsv.Split();  //split into components
                            Image<Gray, Byte>[] imghue = new Image<Gray, byte>[1]; imghue[0] = channels[0];            //hsv, so channels[0] is hue.
                            Image<Gray, Byte> imgval = channels[2];            //hsv, so channels[2] is value.
                            Image<Gray, Byte> imgsat = channels[1];            //hsv, so channels[1] is saturation.

                            mask = new Image<Gray, Byte>(w, h);
                            Hsv hsv_lower = new Hsv(0, smin, Math.Min(vmin, vmax));
                            Hsv hsv_upper = new Hsv(180, 256, Math.Max(vmin, vmax));
                            mask = hsv.InRange(hsv_lower, hsv_upper);

                            Image<Gray, Byte> backproject = Main.hist.BackProject(imghue);

                            mask = mask.And(finalBlobImg.Dilate(2));
                            backproject = mask.And(backproject);
                            MCvConnectedComp trac_comp = new MCvConnectedComp();
                            //Console.WriteLine("2");
                            MCvTermCriteria criteria_mean = new MCvTermCriteria(100, 0.002);
                            pictureBox2.Image = mask.Bitmap;
                            //Console.WriteLine(criteria_mean.GetType);                
                            try
                            {
                              
                                Emgu.CV.CvInvoke.cvMeanShift(backproject, track_window_mean, criteria_mean, out trac_comp);
                            }
                            catch (CvException e)
                            {
                                Console.WriteLine(track_window_mean);
                                MessageBox.Show(e.ToString());
                            }
                           // Console.WriteLine("3");

                            currImage.Draw(trac_comp.rect, new Bgr(255, 0, 0), 2);
                            currImage.Draw(new Cross2DF(new PointF((trac_comp.rect.X+trac_comp.rect.Width/2),(trac_comp.rect.Y+trac_comp.rect.Height/2)),20,20), new Bgr(255, 255, 255), 2);
                            track_window_mean = trac_comp.rect;

                            //check person left the view
                            Image<Gray, byte> subImgBg = bgImgGy.GetSubRect(trac_comp.rect);
                            Image<Gray, byte> subImgFg = currImgGy.GetSubRect(trac_comp.rect);
                            Image<Gray, byte> imMask = subImgFg.AbsDiff(subImgBg);
                            Gray cnt = imMask.GetAverage();
                            if (cnt.Intensity < 10)
                            {
                                Main.lasttrack = camno;
                                Main.tracking = false;
                                state = "BGSUB";
                                Console.WriteLine("###########" + Thread.CurrentThread.Name + "switches to BGSUB");
                            }
                            //---------------------------
                            outimage = new Image<Gray, byte>(w, h);
                            for (int i = 0; i < trac_comp.rect.Height; i++)
                            {
                                for (int j = 0; j < trac_comp.rect.Width; j++)
                                {

                                    //subImgFg.Data[i, j, 0] = 255;
                                    if (imMask.Data[i, j, 0] < Main.ThSub)
                                    {
                                        imMask.Data[i, j, 0] = 0;
                                        outimage.Data[i + trac_comp.rect.Y, j + trac_comp.rect.X, 0] = 0;
                                    }
                                    else
                                    {
                                        imMask.Data[i, j, 0] = 255;
                                        outimage.Data[i + trac_comp.rect.Y, j + trac_comp.rect.X, 0] = 255;
                                    }
                                }
                                //Console.WriteLine();
                            }

                            outimage._Erode(2);
                            outimage._Dilate(3);
                            try
                            {
                                Image<Bgr, byte> subimg = currImage.And((outimage.Convert<Bgr, Byte>())).GetSubRect(trac_comp.rect);
                                //subimg.GetSubRect(subrect);//.Save(Thread.CurrentThread.Name + "\\" + k++ + ".jpg");

                                //Calc HISTOGRAM of each blob

                                DenseHistogram histBlob = new DenseHistogram(hdims, hranges);//cvCreateHist(1, &hdims, CV_HIST_ARRAY, &hranges, 1);
                                Image<Hsv, byte> hsvBlob = subimg.Convert<Hsv, byte>();

                                //extract the hue and value channels
                                Image<Gray, Byte>[] channelsBlob = hsvBlob.Split();  //split into components
                                Image<Gray, Byte>[] imghueBlob = new Image<Gray, byte>[1]; imghueBlob[0] = channelsBlob[0];            //hsv, so channels[0] is hue.

                                Image<Gray, Byte> maskBlob = hsvBlob.InRange(hsv_lower, hsv_upper);

                                histBlob.Calculate(imghueBlob, false, maskBlob);

                                double distance = CvInvoke.cvCompareHist(Main.hist.Ptr, histBlob.Ptr, Emgu.CV.CvEnum.HISTOGRAM_COMP_METHOD.CV_COMP_BHATTACHARYYA);
                                //if (distance < 0.15)
                                //{
                                //  //  Main.hist = histBlob;
                                //    Console.WriteLine(Thread.CurrentThread.Name + " ===== " + distance);
                                //}
                            
                            }

                            catch (CvException cve)
                            {
                                     MessageBox.Show(cve.StackTrace);
                            }
                            //---------------------------

                            //pictureBox2.Image = mask.Bitmap;
                            //pictureBox3.Image = mask.And(finalBlobImg).Bitmap;
                            break;


                    }
                    pictureBox1.Image = currImage.Bitmap;
                    

                    Thread.Sleep(20);
                }
                Console.WriteLine("###########" + Thread.CurrentThread.Name + " exited");
            }catch(CvException e)
            {}
        }



        public ArrayList bgSubtraction(Image<Bgr, Byte> currImage, Image<Bgr, Byte> bgImage, ref Image<Gray, byte> finalBlobImg, ref ArrayList blobRect)
        {
            //### inputs are currImage and bgImage. Output is finalBlobImg - 1 for FG and 0 for BG and blobRect to store ROIs of each blob in this frame
            //### extracting pixels from currImage and bgImage
            byte[, ,] curImgPix = new byte[Main.w, Main.h, 1];
            byte[, ,] bw_2d = new byte[Main.h / Main.b, Main.w / Main.b, 1];
            ArrayList blobDistanceList = new ArrayList(1);

            curImgPix = bitmap22D(currImage);
            currImgGy = currImage.Convert<Gray, byte>();
            bgImgGy = bgImage.Convert<Gray, byte>();

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
                if (blob.Count > 25)   //no of blocks in each blob > 3
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
                    currImage.Draw(subrect, new Bgr(0, 0, 0), 1);

                    Image<Gray, byte> subImgBg = bgImgGy.GetSubRect(subrect);
                    Image<Gray, byte> subImgFg = currImgGy.GetSubRect(subrect);

                    //subImgBg.Save("bg.jpg");
                    //subImgFg.Save("fg"+k+++".jpg");

                    Image<Gray, byte> imMask = subImgFg.AbsDiff(subImgBg);
                    //Console.WriteLine(Thread.CurrentThread.Name + " " + subrect);


                    for (int i = 0; i < subrect.Height; i++)
                    {
                        for (int j = 0; j < subrect.Width; j++)
                        {

                            //subImgFg.Data[i, j, 0] = 255;
                            if (imMask.Data[i, j, 0] < Main.ThSub)
                            {
                                imMask.Data[i, j, 0] = 0;
                                outimage.Data[i + subrect.Y, j + subrect.X, 0] = 0;
                            }
                            else
                            {
                                imMask.Data[i, j, 0] = 255;
                                outimage.Data[i + subrect.Y, j + subrect.X, 0] = 255;
                            }
                        }
                        //Console.WriteLine();
                    }

                    //imMask._Erode(1);
                    //imMask._Dilate(2);

                    outimage._Erode(2);
                    outimage._Dilate(3);
                    try
                        {
                            Image<Bgr, byte> subimg = currImage.And((outimage.Convert<Bgr, Byte>())).GetSubRect(subrect);
                            //subimg.GetSubRect(subrect);//.Save(Thread.CurrentThread.Name + "\\" + k++ + ".jpg");

                            //Calc HISTOGRAM of each blob

                            DenseHistogram histBlob = new DenseHistogram(hdims, hranges);//cvCreateHist(1, &hdims, CV_HIST_ARRAY, &hranges, 1);
                            Image<Hsv, byte> hsvBlob = subimg.Convert<Hsv, byte>();

                            //extract the hue and value channels
                            Image<Gray, Byte>[] channelsBlob = hsvBlob.Split();  //split into components
                            Image<Gray, Byte>[] imghueBlob = new Image<Gray, byte>[1]; imghueBlob[0] = channelsBlob[0];            //hsv, so channels[0] is hue.

                            Hsv hsv_lower = new Hsv(0, smin, Math.Min(vmin, vmax));
                            Hsv hsv_upper = new Hsv(180, 256, Math.Max(vmin, vmax));
                            Image<Gray, Byte> maskBlob = hsvBlob.InRange(hsv_lower, hsv_upper);

                            histBlob.Calculate(imghueBlob, false, maskBlob);

                            double distance = CvInvoke.cvCompareHist(Main.hist.Ptr, histBlob.Ptr, Emgu.CV.CvEnum.HISTOGRAM_COMP_METHOD.CV_COMP_BHATTACHARYYA);

                            //Console.WriteLine(Thread.CurrentThread.Name + " = " + distance);
                           

                        //Add rect and distance of each blob into blobDistanceList
                            Blob currenBlob;
                            currenBlob.rect=subrect;
                            currenBlob.distance=distance;
                            blobDistanceList.Add(currenBlob);                        
                    }
                
                        catch (CvException cve)
                        {
                            //     MessageBox.Show(cve.StackTrace);
                        }
                    
                    finalBlobImg = finalBlobImg.Or(outimage);
                }
            }
            return blobDistanceList;
        }

        public void motionDetection(byte[, ,] one_2d, byte[, ,] two_2d, int w, int h, int b, ref byte[, ,] bw_2d)
        {

            for (int r = 0; r < (h - b + 1); r = r + b)            //(h-b+1);r=r+blk)
            {
                for (int c = 0; c < (w - b + 1); c = c + b)             //(w-b+1);c=c+blk)
                {
                    //move to each blocks in the current frame
                    if (subtraction(one_2d, r, c, two_2d, r, c, b) > Main.ThBg)
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
                    compLabel(i - 1, j - 1, m, xy, ref bw_2d);
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
    
    }
}