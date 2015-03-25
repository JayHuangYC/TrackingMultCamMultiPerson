using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PeopleDetection
{
    class Manager
    {
        public CamProcess[] cam = new CamProcess[2];
        public static bool tracking = true;
        public static int lasttrack = -1;
        public Manager(CamProcess cam1, CamProcess cam2, CamProcess cam3)
        {
            this.cam[0] = cam1;
            this.cam[1]= cam2;
            //this.cam[2] = cam3;

        }
        public void run()
        {
            //to shift the tracker to new cameras and set back the tracker camera to BGSUB state
            while (true)
            {
                while (!tracking)
                {
                  //  MessageBox.Show("TEST1");
                    for (int i = 0; i < cam.Length; i++)
                    {
                      //  MessageBox.Show("TEST2");
                        String state = cam[i].state;
                        switch (state)
                        {
                            case "BGSUB":
                              //  MessageBox.Show("TEST3");
                                if (cam[i].blobDistanceList != null)
                                {
                                    foreach (Blob blob in cam[i].blobDistanceList)
                                    {
                                        if (blob.distance < 0.35)
                                        {                                            
                                            cam[i].track_window_mean = blob.rect;
                                            cam[i].track_window_mean.Width = blob.rect.Width +20;            
                                            lasttrack = i;
                                            cam[i].state = "TRACK";
                                            break;
                                        }
                                    }
                                }

                                break;
                        }
                    }
                }
            }
        }
            }
        }
            

       

