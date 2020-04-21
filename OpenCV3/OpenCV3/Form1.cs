using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using Emgu.CV;                  //
using Emgu.CV.CvEnum;           // usual Emgu CV imports
using Emgu.CV.Structure;        //
using Emgu.CV.UI;
using Emgu.Util;
using Emgu.CV.Util;
//


namespace OpenCV3
{
    public partial class Form1 : Form
    {
        Capture capWebcam;
        CascadeClassifier haar;
        Form2 info = new Form2();
        bool logic = false;
        int cont1 = 0;
        int cont2 = 0;
        int cont3 = 0;
        int cont4 = 0;
        int cont5 = 0;


        public Form1()
        {
            InitializeComponent();

            info.Owner = this;
            info.Show();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                capWebcam = new Capture(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("unable to read from webcam, error: " + Environment.NewLine + Environment.NewLine +
                                ex.Message + Environment.NewLine + Environment.NewLine +
                                "exiting program");
                Environment.Exit(0);
                return;
            }
            Application.Idle += processFrameAndUpdateGUI;       // add process image function to the application's list of tasks
            haar = new CascadeClassifier("haarcascade_frontalface_alt.xml");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        void processFrameAndUpdateGUI(object sender, EventArgs arg)
        {
            Mat imgOriginal;
          

            imgOriginal = capWebcam.QueryFrame();

            if (imgOriginal == null)
            {
                MessageBox.Show("unable to read frame from webcam" + Environment.NewLine + Environment.NewLine +
                                "exiting program");
                Environment.Exit(0);
                return;
            }

            Image<Rgb, Byte> imageOriginal = imgOriginal.ToImage<Rgb, Byte>();
            var rectangles = haar.DetectMultiScale(imageOriginal, 1.1,3);
            string s = string.Join("\n", rectangles.Select((rect) => rect.X + " " + rect.Y + " " + rect.Width + " " + rect.Height));

            string[] arr = s.Split(' ');

            Mat imgGrayscale = new Mat(imgOriginal.Size, DepthType.Cv8U, 1);
            Mat imgBlurred = new Mat(imgOriginal.Size, DepthType.Cv8U, 1);
            Mat imgCanny = new Mat(imgOriginal.Size, DepthType.Cv8U, 1);

            CvInvoke.CvtColor(imgOriginal, imgGrayscale, ColorConversion.Bgr2Gray);

            CvInvoke.GaussianBlur(imgGrayscale, imgBlurred, new Size(5, 5), 1.5);

            CvInvoke.Canny(imgBlurred, imgCanny, 180, 100);

            var contours = new VectorOfVectorOfPoint();
            var contours_Of_man = new VectorOfVectorOfPoint();

            CvInvoke.FindContours(imgCanny, contours, null, RetrType.Tree, ChainApproxMethod.ChainApproxSimple);

            try
            {
                //получаем данные о прямоугольнике - координаты левого нижнего угла и размеры
                int X = Convert.ToInt32(arr[0]);
                int Y = Convert.ToInt32(arr[1]);
                int W = Convert.ToInt32(arr[2]);
                int H = Convert.ToInt32(arr[3]);
                //вычисляем координаты всех четырех углов
                int x1, y1, x2, y2, x3, y3, x4, y4;
                x1 = X;
                y1 = Y;
                x2 = X;
                y2 = Y+H;
                x3 = X + W;
                y3 = Y +H;
                x4 = X+W;
                y4 = Y;

                double left, right, up, down, center_1_x, center_1_y, center_2_x, center_2_y;

                //находим координаты центра лица и центра изображения
                center_1_x = Convert.ToDouble(X) + (Convert.ToDouble(W)) / 2.0;
                center_1_y = Convert.ToDouble(Y) + (Convert.ToDouble(H)) / 2.0;
                center_2_x = (Convert.ToDouble(ibOriginal.Width))/2.0;
                center_2_y = (Convert.ToDouble(ibOriginal.Height))/2.0;

                //вычисляем на сколько в градусах надо повернуть камеру чтобы центры лица и изображения совпали
                if (center_1_x > center_2_x)
                {
                    left = 0.0;
                    right = (center_1_x - center_2_x) / ((Convert.ToDouble(ibOriginal.Width)) / 50.0);
                }
                else
                {
                    right = 0.0;
                    left = (center_2_x - center_1_x) / ((Convert.ToDouble(ibOriginal.Width)) / 50.0);
                }
                if (center_1_y > center_2_y)
                {
                    down = 0.0;
                    up = (center_1_y - center_2_y) / ((Convert.ToDouble(ibOriginal.Height)) / 45.0);
                }
                else
                {
                    up = 0.0;
                    down = (center_2_y - center_1_y) / ((Convert.ToDouble(ibOriginal.Height)) / 45.0);
                }

                //передаем данные на вторую форму 
                CallBackMy.callbackEventHandler(left, right, down, up);

                //выделяем лицо прямоугольником
                Bitmap image1;

                image1 = new Bitmap(imgOriginal.Bitmap);
                Graphics g1 = Graphics.FromImage(image1);
                g1.DrawLine(new Pen(Brushes.Red, 2), x1, y1, x2, y2);
                g1.DrawLine(new Pen(Brushes.Red, 2), x2, y2, x3, y3);
                g1.DrawLine(new Pen(Brushes.Red, 2), x3, y3, x4, y4);
                g1.DrawLine(new Pen(Brushes.Red, 2), x4, y4, x1, y1);

                Image<Rgb, Byte> imageOriginal1 = new Image<Rgb, Byte>(image1);

                //выбираем контуры, которые принадлежат человеку.

                int x_cont_man_1, x_cont_man_2, y_cont_man_1, y_cont_man_2;

                x_cont_man_1 = X -W;
                x_cont_man_2 = X + 2*W;
                y_cont_man_1 = Y-H/2;
                y_cont_man_2 = Y+6*H;

                contours_Of_man.Clear();

                for (int i = 0; i < contours.Size; i++)
                {
                    bool logic1 = true;
                    for (int j = 0; j < contours[i].Size; j++)
                    {
                        if (contours[i][j].X < x_cont_man_1 || contours[i][j].X > x_cont_man_2 || contours[i][j].Y < y_cont_man_1 || contours[i][j].Y > y_cont_man_2)
                        {
                            logic1 = false;
                            j = contours[i].Size;
                        }
                    }
                    if (logic1)
                    {
                        VectorOfPoint mat = new VectorOfPoint();
                        mat = contours[i];
                        contours_Of_man.Push(mat);
                    }
                }
                Mat imagCanny = new Mat(imgOriginal.Size, DepthType.Cv8U, 1);
                imagCanny.SetTo(new MCvScalar(0, 0, 0));
                for (int h = 0; h < contours_Of_man.Size; h++)
                {
                   CvInvoke.DrawContours(imagCanny, contours_Of_man, h, new MCvScalar(255, 0, 255, 0));
                }
                
                ibOriginal.Image = imageOriginal1;
                ibCanny.Image = imagCanny;
                logic = true;
            }
            catch (Exception ea)
            {

                CallBackMy.callbackEventHandler(100.0, 100.0, 100.0, 100.0);
                if (logic)
                {
                    Mat imagCanny = new Mat(imgOriginal.Size, DepthType.Cv8U, 1);
                    imagCanny.SetTo(new MCvScalar(0, 0, 0));
                    for (int h = 0; h < contours.Size; h++)
                    {
                          CvInvoke.DrawContours(imagCanny, contours, h, new MCvScalar(255, 0, 255, 0));
                    }
                    ibOriginal.Image = imgOriginal;
                    ibCanny.Image = imagCanny;
                }
                else
                {
                    ibOriginal.Image = imgOriginal;
                    ibCanny.Image = imgCanny;
                }
            }
        }
    }
}