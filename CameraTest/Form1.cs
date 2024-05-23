using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;

namespace CameraTest
{
    public partial class Form1 : Form
    {
        Timer t1;
        VideoCapture vc = new VideoCapture(0);
        VideoWriter vw = new VideoWriter();

        Mat frame = new Mat();

        BarcodeReader bReader = new BarcodeReader();
        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            t1 = new Timer();
            t1.Interval = 100;
            t1.Start();
            t1.Tick += (_, args) =>
            {
                vc.Read(frame);
                vw.Write(frame);

                pictureBox1.Image = Bitmap.FromStream(frame.ToMemoryStream());

                //테두리 그리기
                Result bResult = bReader.Decode((Bitmap)pictureBox1.Image);
                if (bResult == null)
                {
                    return;
                }
                System.Drawing.Point p1 = new System.Drawing.Point(Convert.ToInt32(bResult.ResultPoints[0].X), Convert.ToInt32(bResult.ResultPoints[0].Y));
                panel1.BorderStyle = BorderStyle.FixedSingle;
                panel1.Location = p1;
                panel1.Size = new System.Drawing.Size(10, 10);
            };
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string save = DateTime.Now.ToString("yyyy-MM-dd-hhmmss");
            string path = @"D:\TestProject\CameraTest\";

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                Cv2.ImWrite(path + save + ".png", frame);
                MessageBox.Show("저장에 성공했습니다.");
            }
            catch (Exception Ex)
            {
                MessageBox.Show("저장에 실패했습니다." + Ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string decoded;
            Result bResult = bReader.Decode((Bitmap)pictureBox1.Image);

            try
            {
                if (bResult != null)
                {
                    decoded = "Decode : " + bResult.ToString() + "\r\n" +
                              "Foramt : " + bResult.BarcodeFormat.ToString();
                    if (decoded != "")
                    {
                        richTextBox1.Text = decoded;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
