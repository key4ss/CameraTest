using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Text.RegularExpressions;
using AForge.Video;
using System.Diagnostics;
using AForge.Video.DirectShow;
using System.Collections;
using System.IO;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Globalization;
using System.Net;
using ZXing;
using System.Drawing.Drawing2D;

namespace CameraTest
{
    public partial class Form2 : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoDevice;
        private VideoCapabilities[] snapshotCapabilities;
        private ArrayList listCamera = new ArrayList();
        //public string pathFolder = Application.StartupPath + @"\ImageCapture\"; // Sample img 호출용 -> 미사용
        private bool isCameraOpened = false;
        private static bool needSnapshot = false;
        private bool barcodeScanmode = false;
        int barcodeRectangleLeft = 0;
        int barcodeRectangleRight = 0;
        int barcodeRectangleTop = 0;
        int barcodeRectangleBottom = 0;

        public Form2()
        {
            InitializeComponent();
            //this.MaximizeBox = false; // 최대화 버튼 비활성화
            getListCameraUSB();
        }

        private static string _usbcamera;
        public string usbcamera
        {
            get { return _usbcamera; }
            set { _usbcamera = value; }
        }

        // on/off btn
        private void button1_Click(object sender, EventArgs e)
        {
            if (isCameraOpened)
            {
                CloseCamera();
            }
            else
            {
                OpenCamera();
            }
        }

        #region Open Scan Camera
        private void OpenCamera()
        {
            try
            {
                usbcamera = comboBox1.SelectedIndex.ToString();
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count != 0)
                {
                    foreach (FilterInfo device in videoDevices)
                    {
                        listCamera.Add(device.Name);

                    }
                }
                else
                {
                    MessageBox.Show("Camera devices found");
                }

                videoDevice = new VideoCaptureDevice(videoDevices[Convert.ToInt32(usbcamera)].MonikerString);
                snapshotCapabilities = videoDevice.SnapshotCapabilities;

                //if (snapshotCapabilities.Length == 0)
                //{
                //    //MessageBox.Show("Camera Capture Not supported");
                //    //이 부분 주석해제하면 기능에는 문제없지만 지속적으로 발생.
                //}

                OpenVideoSource(videoDevice);
            }
            catch (Exception ex)
            {
                MessageBox.Show("openCameraException", ex.Message);
            }

        }
        #endregion

        #region Close Scan Camera
        private void CloseCamera()
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                videoSourcePlayer1.Paint -= videoSourcePlayer1_Paint;
                videoSourcePlayer1.Invalidate();

                videoSourcePlayer1.VideoSource = videoDevice;
                videoSourcePlayer1.Stop();

                isCameraOpened = false;
                barcodeScanmode = false;

                this.Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                MessageBox.Show("closeCamera", ex.Message);
            }
        }
        #endregion


        public delegate void CaptureSnapshotManifast(Bitmap image);

        public void UpdateCaptureSnapshotManifast(Bitmap image)
        {
            try
            {
                needSnapshot = false;

                Bitmap copyedImage = (Bitmap)image.Clone();

                // Resizing copyed image to pictureBox size
                Bitmap resizedImage = new Bitmap(pictureBox2.Width, pictureBox2.Height);
                using (Graphics g = Graphics.FromImage(resizedImage))
                {
                    g.DrawImage(copyedImage, 0, 0, pictureBox2.Width, pictureBox2.Height);
                }

                // Display the resized image
                pictureBox2.Image = resizedImage;
                pictureBox2.Update();

                #region load sample image
                //string namaImage = "sampleImage";
                //string nameCapture = namaImage + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bmp";

                //if (Directory.Exists(pathFolder))
                //{
                //    resizedImage.Save(pathFolder + nameCapture, ImageFormat.Bmp);
                //}
                //else
                //{
                //    Directory.CreateDirectory(pathFolder);
                //    resizedImage.Save(pathFolder + nameCapture, ImageFormat.Bmp);
                //}
                #endregion
            }

            catch (Exception ex)
            {
                MessageBox.Show("snapShotManifast", ex.Message);
            }

        }

        public void OpenVideoSource(IVideoSource source)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                CloseCurrentVideoSource();

                videoSourcePlayer1.VideoSource = source;
                videoSourcePlayer1.Start();

                isCameraOpened = true;

                this.Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                MessageBox.Show("openVideoSource", ex.Message);
            }
        }

        private void getListCameraUSB()
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count != 0)
            {
                foreach (FilterInfo device in videoDevices)
                {
                    comboBox1.Items.Add(device.Name);

                }
            }
            else
            {
                comboBox1.Items.Add("No DirectShow devices found");
            }

            comboBox1.SelectedIndex = 0;

        }

        public void CloseCurrentVideoSource()
        {
            try
            {

                if (videoSourcePlayer1.VideoSource != null)
                {
                    videoSourcePlayer1.SignalToStop();

                    for (int i = 0; i < 30; i++)
                    {
                        if (!videoSourcePlayer1.IsRunning)
                            break;
                        System.Threading.Thread.Sleep(100);
                    }

                    if (videoSourcePlayer1.IsRunning)
                    {
                        videoSourcePlayer1.Stop();
                    }

                    videoSourcePlayer1.VideoSource = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("closeVideoSource", ex.Message);
            }
        }

        // capture btn
        private void button2_Click(object sender, EventArgs e)
        {
            needSnapshot = true;
        }

        private void videoSourcePlayer1_NewFrame_1(object sender, ref Bitmap image)
        {
            try
            {
                DateTime now = DateTime.Now;
                Graphics g = Graphics.FromImage(image);

                // insert current time
                SolidBrush brush = new SolidBrush(Color.Red);
                Font font = new Font("굴림", 24);
                g.DrawString(now.ToString(), font, brush, new PointF(5, 5));
                brush.Dispose();

                //Snapshot
                if (needSnapshot)
                {
                    this.Invoke(new CaptureSnapshotManifast(UpdateCaptureSnapshotManifast), image);
                }

                //Barcode
                BarcodeReader barcodeReader = new BarcodeReader();
                Result result = barcodeReader.Decode(image);
                if (barcodeScanmode = true && result != null)
                {  
                    if(result.BarcodeFormat == BarcodeFormat.QR_CODE)
                    {
                        PointF[] points = Array.ConvertAll(result.ResultPoints,
                        new Converter<ResultPoint, PointF>((point) => new PointF((float)point.X, (float)point.Y)));

                        Pen pen = new Pen(Color.Red, 5);
                        g.DrawPolygon(pen, points);
                        pen.Dispose();
                    }
                    else if(lineBarcodeFormats.Contains(result.BarcodeFormat))
                    {
                        PointF topStartPoint = new PointF((float)result.ResultPoints[0].X, (float)result.ResultPoints[0].Y);
                        PointF topEndPoint = new PointF((float)result.ResultPoints[1].X, (float)result.ResultPoints[1].Y);

                        Pen pen = new Pen(Color.Blue, 5);
                        g.DrawLine(pen, topStartPoint, topEndPoint);

                        pen.Dispose();
                    }

                    textBox1.Invoke((MethodInvoker)delegate { textBox1.Text = result.Text; });
                }

                g.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("captureManifast", ex.Message);
            }
        }

        // save btn
        private void button3_Click(object sender, EventArgs e)
        {
            if (pictureBox2.Image != null)
            {
                string fileName = DateTime.Now.ToString("yyyy-MM-dd-hhmmss") + ".bmp";
                string path = @"D:\CameraTest\";

                try
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    string filePath = Path.Combine(path, fileName);
                    Bitmap capturedImage = new Bitmap(pictureBox2.Image);
                    capturedImage.Save(filePath, ImageFormat.Bmp);
                    MessageBox.Show("Image saving was successful.");

                    //test
                    byte[] imageBytes;
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        capturedImage.Save(memoryStream, ImageFormat.Bmp);
                        imageBytes = memoryStream.ToArray();
                    }
                    string base64String = Convert.ToBase64String(imageBytes);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to saving image.\rn" + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Image was not found.");
            }
        }

        // reset btn
        private void button4_Click(object sender, EventArgs e)
        {
            if (pictureBox2.Image != null)
            {
                pictureBox2.Image = null;
            }

            if (textBox1.Text != null)
            {
                textBox1.Text = "";
            }
        }

        private List<BarcodeFormat> lineBarcodeFormats = new List<BarcodeFormat>
        {
            BarcodeFormat.UPC_A,
            BarcodeFormat.UPC_E,
            BarcodeFormat.EAN_8,
            BarcodeFormat.EAN_13,
            BarcodeFormat.CODE_128
        };

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            videoSourcePlayer1.SignalToStop();
        }

        // BarcodeScan
        private void button5_Click(object sender, EventArgs e)
        {
            if (isCameraOpened)
            {
                videoSourcePlayer1.Paint -= videoSourcePlayer1_Paint;
                videoSourcePlayer1.Paint += new System.Windows.Forms.PaintEventHandler(this.videoSourcePlayer1_Paint);
                videoSourcePlayer1.Invalidate();
                barcodeScanmode = true;
            }
            else
            {
                MessageBox.Show("Please turn on the camera first");
            }
        }

        private void videoSourcePlayer1_Paint(object sender, PaintEventArgs e)
        {
            // videoSourcePlayer1의 크기와 중앙 좌표 구하기
            int playerWidth = videoSourcePlayer1.Width;
            int playerHeight = videoSourcePlayer1.Height;
            int centerX = playerWidth / 2;
            int centerY = playerHeight / 2;

            // 중앙에 사각형을 그리기 위한 영역 계산
            int rectWidth = 350;
            int rectHeight = 150;
            int rectX = centerX - (rectWidth / 2);
            int rectY = centerY - (rectHeight / 2);

            // 중앙에 위치한 사각형을 그리기
            Rectangle barcodeRectangle = new Rectangle(rectX, rectY, rectWidth, rectHeight);
            e.Graphics.DrawRectangle(Pens.White, barcodeRectangle);

            barcodeRectangleLeft = rectX;
            barcodeRectangleRight = rectX + rectWidth;
            barcodeRectangleTop = rectY;
            barcodeRectangleBottom = rectY + rectHeight;

            // barcodeRectangle 영역 제외한 나머지 영역 투명도 조절.
            GraphicsPath path = new GraphicsPath();
            path.AddRectangle(new Rectangle(0, 0, playerWidth, playerHeight));
            path.AddRectangle(barcodeRectangle);
            Region region = new Region(path);
            e.Graphics.FillRegion(new SolidBrush(Color.FromArgb(128, Color.Black)), region);
        }
    }
}
