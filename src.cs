using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;
using Accord.Statistics.Kernels;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using ZedGraph;

namespace AmharicLetters_HWR
{
    public partial class Form1 : Form
    {
        MulticlassSupportVectorMachine ksvm;
        
        public Form1()
        {
            InitializeComponent();
        }
        
        #region Data Preprocessing & Extraction
        private Bitmap Extract(string text)
        {
            // Format32bppRgb: a pixel format representing 32 bits per pixel RGB color; 8 bits for R, G, B and the
            // rest 8 bits unused.
            Bitmap bitmap = new Bitmap(32, 32, PixelFormat.Format32bppRgb);
            string[] lines = text.Split(new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    if (lines[i][j] == '0')
                        bitmap.SetPixel(j, i, Color.White);
                    else bitmap.SetPixel(j, i, Color.Black);
                }
            }
            return bitmap;
        }

        private double[] Extract(Bitmap bmp)
        {
            double[] features = new double[32 * 32];
            for (int i = 0; i < 32; i++)
                for (int j = 0; j < 32; j++)
                    features[i * 32 + j] = (bmp.GetPixel(j, i).R == 255) ? 0 : 1;

            return features;
        }

        private Bitmap Export(double[] features)
        {
            Bitmap bitmap = new Bitmap(32, 32, PixelFormat.Format32bppRgb);

            for (int i = 0; i < 32; i++)
                for (int j = 0; j < 32; j++)
                {
                    double v = features[i * 32 + j];
                    v = 255 - Math.Max(0, Math.Min(255, Math.Abs(v) * 255));
                    bitmap.SetPixel(j, i, Color.FromArgb((int)v, (int)v, (int)v));
                }

            return bitmap;
        }

        private double[] Preprocess(Bitmap bitmap)
        {
            double[] features = new double[64];

            for (int m = 0; m < 8; m++)
            {
                for (int n = 0; n < 8; n++)
                {
                    int c = m * 8 + n;
                    for (int i = m * 4; i < m * 4 + 4; i++)
                    {
                        for (int j = n * 4; j < n * 4 + 4; j++)
                        {
                            Color pixel = bitmap.GetPixel(j, i);
                            if (pixel.R == 0x00) // white
                                features[c] += 1;
                        }
                    }
                }
            }

            return features;
        }
        #endregion

        #region Form Actions
        private void btnSampleRunAnalysis_Click(object sender, EventArgs e)
        {
            trainMachine();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            loadData();
        }

        private void btnClassify_Click(object sender, EventArgs e)
        {
            testMachine();   
        }

        private void button2_Click(object sender, EventArgs e)
        {
            classifyHandwriting();
        }

        private void button3_Click(object sender, EventArgs e)
        {
           canvas.Clear();
        }

        private void tbPenWidth_Scroll(object sender, EventArgs e)
        {
            canvas.PenSize = tbPenWidth.Value;
        }

        private void canvas_MouseUp(object sender, MouseEventArgs e)
        {
            button2_Click(this, EventArgs.Empty);
        }

        private void load_data_Click(object sender, EventArgs e)
        {
            loadData();
        }
        #endregion

        #region ZedGraph Creation

        public void CreateBarGraph(ZedGraphControl zgc, double[] discriminants)
        {
            GraphPane myPane = zgc.GraphPane;

            myPane.CurveList.Clear();

            myPane.Title.IsVisible = false;
            myPane.Legend.IsVisible = false;
            myPane.Border.IsVisible = false;
            myPane.Border.IsVisible = false;
            myPane.Margin.Bottom = 20f;
            myPane.Margin.Right = 20f;
            myPane.Margin.Left = 20f;
            myPane.Margin.Top = 30f;

            myPane.YAxis.Title.IsVisible = true;
            myPane.YAxis.IsVisible = true;
            myPane.YAxis.MinorGrid.IsVisible = false;
            myPane.YAxis.MajorGrid.IsVisible = false;
            myPane.YAxis.IsAxisSegmentVisible = false;
            myPane.YAxis.Scale.Max = 4.5;
            myPane.YAxis.Scale.Min = -0.5;
            myPane.YAxis.MajorGrid.IsZeroLine = false;
            myPane.YAxis.Title.Text = "ፊደላት";
            myPane.YAxis.MinorTic.IsOpposite = false;
            myPane.YAxis.MajorTic.IsOpposite = false;
            myPane.YAxis.MinorTic.IsInside = false;
            myPane.YAxis.MajorTic.IsInside = false;
            myPane.YAxis.MinorTic.IsOutside = false;
            myPane.YAxis.MajorTic.IsOutside = false;

            myPane.XAxis.MinorTic.IsOpposite = false;
            myPane.XAxis.MajorTic.IsOpposite = false;
            myPane.XAxis.Title.IsVisible = true;
            myPane.XAxis.Title.Text = "Relative class response";
            myPane.XAxis.IsVisible = true;
            myPane.XAxis.Scale.Min = 0;
            myPane.XAxis.Scale.Max = 100;
            myPane.XAxis.IsAxisSegmentVisible = false;
            myPane.XAxis.MajorGrid.IsVisible = false;
            myPane.XAxis.MajorGrid.IsZeroLine = false;
            myPane.XAxis.MinorTic.IsOpposite = false;
            myPane.XAxis.MinorTic.IsInside = false;
            myPane.XAxis.MinorTic.IsOutside = false;
            myPane.XAxis.Scale.Format = "0'%";


            // Create data points for three BarItems using Random data
            PointPairList list = new PointPairList();

            for (int i = 0; i < discriminants.Length; i++)
                list.Add(discriminants[i] * 100, i);

            BarItem myCurve = myPane.AddBar("b", list, Color.DarkBlue);


            // Set BarBase to the YAxis for horizontal bars
            myPane.BarSettings.Base = BarBase.Y;


            zgc.AxisChange();
            zgc.Invalidate();

        }
        #endregion

        #region Common Methods
        private String getAmharicLetter(int num) {
            switch (num)
            {
                case 0:
                    return "ሀ";
                case 1:
                    return "ለ";
                case 2:
                    return "ሐ";
                case 3:
                    return "መ";
                case 4:
                    return "ሠ";
                default:
                    return "Invalid";
            }
        }

        private void loadData()
        {
            lbStatus.Text = "Loading data. This may take a while...";
            int trainingCount = loadTrainingData();
            int testingCount = loadTestingData();
            lbStatus.Text = String.Format(
                "Dataset loaded. Click Run training to start the training.",
                trainingCount, testingCount);

            btn_train_machine.Enabled = true;
        }

        private int loadTrainingData()
        {
            //lbStatus.Text = "Loading data. This may take a while...";
            Application.DoEvents();
            
            // Load amharic letters training dataset into the DataGridView
            StringReader reader = new StringReader(Properties.Resources.amharic_letters_train);

            int trainingStart = 0;
            int trainingCount = 500;

            dgvTrainingSource.Rows.Clear();
            
            int c = 0;
            while (true)
            {
                // /r/n are also counted as character. That's why [34*32]
                char[] buffer = new char[(32 + 2) * 32];
                int read = reader.ReadBlock(buffer, 0, buffer.Length);

                // read the line after the [34*32] array. i.e the training result.
                string label = reader.ReadLine();

                if (read < buffer.Length || label == null) break;

                // If it started & didn't finish the training:
                if (c > trainingStart && c <= trainingStart + trainingCount)
                {
                    // Extract bitmap image from the string buffer.
                    Bitmap bitmap = Extract(new String(buffer));

                    // Extract array of bits from bitmap image.
                    double[] features = Extract(bitmap);

                    // Parse the result label to integer.
                    int clabel = Int32.Parse(label);

                    String letter = getAmharicLetter(clabel);

                    // Add as row to DataGridView.
                    dgvTrainingSource.Rows.Add(bitmap, letter, clabel, features);
                }
                
                c++;
            }

            return trainingCount;
        }

        private int loadTestingData()
        {
            //lbStatus.Text = "Loading data. This may take a while...";
            Application.DoEvents();

            // Load amharic letters testing dataset into the DataGridView
            StringReader reader = new StringReader(Properties.Resources.amharic_letters_test);

            int testingStart = 0;
            int testingCount = 500;

            dgvAnalysisTesting.Rows.Clear();

            int c = 0;
            while (true)
            {
                // /r/n are also counted as character. That's why [34*32]
                char[] buffer = new char[(32 + 2) * 32];
                int read = reader.ReadBlock(buffer, 0, buffer.Length);

                // read the line after the [34*32] array. i.e the training result.
                string label = reader.ReadLine();

                if (read < buffer.Length || label == null) break;

                // If it started but didn't finish the testing:
                if (c > testingStart && c <= testingStart + testingCount)
                {
                    Bitmap bitmap = Extract(new String(buffer));
                    double[] features = Extract(bitmap);
                    int clabel = Int32.Parse(label);
                    String letter = getAmharicLetter(clabel);
                    dgvAnalysisTesting.Rows.Add(bitmap, letter, clabel, null, features);
                }

                c++;
            }

            return testingCount;
        }

        private void trainMachine() {
            // If DataGridView is empty:
            if (dgvTrainingSource.Rows.Count == 0)
            {
                MessageBox.Show("Please load the training data before clicking this button");
                return;
            }

            lbStatus.Text = "Gathering data. This may take a while...";
            Application.DoEvents();

            // Extract inputs and outputs
            int rows = dgvTrainingSource.Rows.Count;
            double[][] input = new double[rows][];
            int[] output = new int[rows];
            for (int i = 0; i < rows; i++)
            {
                input[i] = (double[])dgvTrainingSource.Rows[i].Cells["colTrainingFeatures"].Value;
                output[i] = (int)dgvTrainingSource.Rows[i].Cells["colTrainingLabel"].Value;
            }

            /*
               There are two types of kernels: Gaussian & Polynomial. 
               Here, we are using the Polynomial Kernel for this app.
               with default values: degree = 2, constant = 0
            */
            IKernel kernel = new Polynomial(2, 0);

            // Create the Multi-class Support Vector Machine using the selected Kernel
            ksvm = new MulticlassSupportVectorMachine(1024, kernel, 5);

            // Create the learning algorithm using the machine and the training data
            MulticlassSupportVectorLearning ml = new MulticlassSupportVectorLearning(ksvm, input, output);

            // Here we've set set default values for complexity, epilson and tolerance.
            double epsilon = 0.001;
            double complexity = 1.0;
            double tolerance = 0.2;

            // Configure the learning algorithm
            ml.Algorithm = (svm, classInputs, classOutputs, i, j) =>
            {
                // SMO is an iterative algorithm for solving the optimization problem described above. 
                // SMO breaks this problem into a series of smallest possible sub-problems, which are
                // then solved analytically.
                var smo = new SequentialMinimalOptimization(svm, classInputs, classOutputs);
                smo.Complexity = complexity;
                smo.Epsilon = epsilon;
                smo.Tolerance = tolerance;
                return smo;
            };


            lbStatus.Text = "Training the classifiers. This may take a (very) significant amount of time...";
            Application.DoEvents();

            Stopwatch sw = Stopwatch.StartNew();

            // Train the machines. It should take a while.
            double error = ml.Run();

            sw.Stop();

            lbStatus.Text = String.Format(
                "Training complete ({0}ms, {1}er). Click Classify to test the classifiers.",
                sw.ElapsedMilliseconds, error);

            btn_test.Enabled = true;
        }

        private void testMachine() {
            if (dgvAnalysisTesting.Rows.Count == 0)
            {
                MessageBox.Show("Please load the testing data before clicking this button");
                return;
            }
            else if (ksvm == null)
            {
                MessageBox.Show("Please perform the training before attempting classification");
                return;
            }

            lbStatus.Text = "Classification started. This may take a while...";
            Application.DoEvents();

            int hits = 0;
            progressBar.Visible = true;
            progressBar.Value = 0;
            progressBar.Step = 1;
            progressBar.Maximum = dgvAnalysisTesting.Rows.Count;

            // Extract inputs
            foreach (DataGridViewRow row in dgvAnalysisTesting.Rows)
            {
                double[] input = (double[])row.Cells["colTestingFeatures"].Value;
                int expected = (int)row.Cells["colTestingExpected"].Value;

                int output = (int)ksvm.Compute(input);
                row.Cells["colTestingOutput"].Value = getAmharicLetter(output);

                if (expected == output)
                {
                    row.Cells[0].Style.BackColor = Color.LightGreen;
                    row.Cells[1].Style.BackColor = Color.LightGreen;
                    row.Cells[2].Style.BackColor = Color.LightGreen;
                    row.Cells[3].Style.BackColor = Color.LightGreen;
                    hits++;
                }
                else
                {
                    row.Cells[0].Style.BackColor = Color.White;
                    row.Cells[1].Style.BackColor = Color.White;
                    row.Cells[2].Style.BackColor = Color.White;
                    row.Cells[3].Style.BackColor = Color.White;
                }

                progressBar.PerformStep();
            }

            progressBar.Visible = false;

            lbStatus.Text = String.Format("Classification complete. Hits: {0}/{1} ({2:0%})",
                hits, dgvAnalysisTesting.Rows.Count, (double)hits / dgvAnalysisTesting.Rows.Count);
        }

        private void classifyHandwriting() {
            if (ksvm != null)
            {
                // Get the input vector drawn
                double[] input = canvas.GetDigit();

                // Classify the input vector
                double[] votes;

                int num = (int)ksvm.Compute(input, out votes);

                // Set the actual classification answer 
                lbCanvasClassification.Text = getAmharicLetter(num);

                // Scale the responses to a [0,1] interval
                double[] responses = new double[votes.Length];
                double max = votes.Max();
                double min = votes.Min();

                for (int i = 0; i < responses.Length; i++)
                    responses[i] = Tools.Scale(min, max, 0, 1, (double)votes[i]);

                // Create the bar graph to show the relative responses
                CreateBarGraph(graphClassification, responses);
            }
        }
        #endregion
    }
}