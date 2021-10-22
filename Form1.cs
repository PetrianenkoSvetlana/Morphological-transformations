using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace Lab4
{
    public partial class Form1 : Form
    {
        Image<Gray, byte> inputImage = null;
        public Form1()
        {
            InitializeComponent();
        }

        private void btnReview_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = openFileDialog1.ShowDialog();

                if (result == DialogResult.OK)
                {
                    inputImage = new Image<Gray, byte>(openFileDialog1.FileName);
                    tbPath.Text = openFileDialog1.FileName;
                    btnCalculate_Click(this, null);
                }
                else
                    MessageBox.Show("Файл не выбран", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            /* Преобразуем картинку для бинаризации цветов */
            inputImage = new Image<Gray, byte>(tbPath.Text);
            for (int x = 0; x < inputImage.Cols; x++)
            {
                for (int y = 0; y < inputImage.Rows; y++)
                {
                    if (inputImage[y, x].Intensity < 150)
                        inputImage[y, x] = new Gray(0);
                    else
                        inputImage[y, x] = new Gray(255);

                }
            }
            pictureBox1.Image = inputImage.Bitmap;

            /* Создаём матрицу для нахождения середин отверстий*/
            Matrix<byte> hole_ring = new Matrix<byte>(92, 92);
            for (int col = 0; col <= hole_ring.Cols * 2 / 3; col++)
            {
                hole_ring[0, hole_ring.Cols / 6 + col] = 1;
                hole_ring[hole_ring.Rows - 1, hole_ring.Cols / 6 + col] = 1;
                hole_ring[hole_ring.Rows / 6 + col, 0] = 1;
                hole_ring[hole_ring.Rows / 6 + col, hole_ring.Cols - 1] = 1;
            }
            for (int i = 0; i < hole_ring.Cols / 6 - 1; i++)
            {
                hole_ring[hole_ring.Cols / 6 - 1 - i, 1 + i] = 1;
                hole_ring[hole_ring.Cols / 6 - 1 - i, hole_ring.Cols - 2 - i] = 1;
                hole_ring[hole_ring.Cols - hole_ring.Cols / 6 + i, 1 + i] = 1;
                hole_ring[hole_ring.Cols - hole_ring.Cols / 6 + i, hole_ring.Cols - 2 - i] = 1;
            }

            /* Находим середины отверстий */
            #region MorphologyEx
            /* operation - оператор морфологической математики */
            /* kernel - элемент обхода */
            /* anchor - смещение середины */
            /* iterations - колическтво итераций */
            /* borderType - тип границы */
            /* borderValue - значение границы */
            #endregion
            Image<Gray, byte> outImage = inputImage.MorphologyEx(MorphOp.HitMiss, hole_ring, new Point(-1, -1), 1,
                BorderType.Default, new MCvScalar());

            pictureBox2.Image = outImage.Bitmap;

            /* Увеличиваем точки середин отверстий и соединяем с первоначальной картинкой */
            #region GetStructuringElement
            /* shape - форма элемента */
            /* ksize - размер элемента */
            /* anchor - смещение середины */
            #endregion
            Mat hole_mask = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(9, 9), new Point(-1, -1));
            outImage = outImage.MorphologyEx(MorphOp.Dilate, hole_mask, new Point(-1, -1), 12,
                BorderType.Default, new MCvScalar());
            pictureBox3.Image = outImage.Bitmap;

            outImage = outImage.Or(inputImage); 
            pictureBox4.Image = outImage.Bitmap;

            /* Отделяем зубцы от дисков */
            Mat gear_body = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(285, 285), new Point(-1, -1));
            outImage = outImage.MorphologyEx(MorphOp.Open, gear_body, new Point(-1, -1), 1,
                BorderType.Default, new MCvScalar());
            pictureBox5.Image = outImage.Bitmap;

            /* Увеличиваем диски*/
            Mat sampling_ring_spacer = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(9, 9), new Point(-1, -1));
            outImage = outImage.MorphologyEx(MorphOp.Dilate, sampling_ring_spacer, new Point(-1, -1), 1,
                BorderType.Default, new MCvScalar());
            pictureBox6.Image = outImage.Bitmap;

            /* Ещё увеличиваем диски и отнимаем предыдущий вариант */
            Mat sampling_ring_width = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(9, 9), new Point(-1, -1));
            var tempImage = outImage.MorphologyEx(MorphOp.Dilate, sampling_ring_width, new Point(-1, -1), 2,
                BorderType.Default, new MCvScalar());
            tempImage -= outImage;
            pictureBox7.Image = tempImage.Bitmap;

            /* Оставляем только зубцы */
            outImage = tempImage.And(inputImage);
            pictureBox8.Image = outImage.Bitmap;

            /* Наращиваем зубцы */
            Mat tip_spacing = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(9, 9), new Point(-1, -1));
            outImage = outImage.MorphologyEx(MorphOp.Dilate, tip_spacing, new Point(-1, -1), 2,
                BorderType.Default, new MCvScalar());
            pictureBox9.Image = outImage.Bitmap;

            /* Оставляем области с отсутствующими зубцами */
            tempImage -= outImage;
            pictureBox10.Image = tempImage.Bitmap;

            /* Увеличиваем области с отсутствующими зубцами, чтобы их было видно */
            Mat defect_cue = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(37, 37), new Point(-1, -1));
            tempImage = tempImage.MorphologyEx(MorphOp.Dilate, defect_cue, new Point(-1, -1), 1,
                BorderType.Default, new MCvScalar());
            pictureBox11.Image = tempImage.Bitmap;

            /* Получаем нужную картинку */
            outImage = tempImage.Or(outImage);
            pictureBox12.Image = outImage.Bitmap;
        }
    }
}
