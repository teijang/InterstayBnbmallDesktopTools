using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Spire.Xls;
using System.Data.SqlClient;
using System.Configuration;

namespace InterstayBnbmallDesktopTools
{
    public partial class FormShippingPrice : Form
    {
        public FormShippingPrice()
        {
            InitializeComponent();

            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;

            btnCencel.Enabled = false;
        }

        private void btnFileSelect_Click(object sender, EventArgs e)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
                    txtFilePath.Text = filePath;

                    //Read xls(x) file
                    Workbook workbook = new Workbook();
                    workbook.LoadFromFile(filePath);

                    Worksheet sheet = workbook.Worksheets[0];

                    dataGridView1.DataSource = sheet.ExportDataTable();

                }

            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
                return;

            if (backgroundWorker1.IsBusy != true)
            {
                // Start the asynchronous operation.
                btnSave.Enabled = false;
                btnCencel.Enabled = true;

                progressBar1.Value = 0;

                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void txtExtraCharge_TextChanged(object sender, EventArgs e)
        {
            for (int i = 1; i < dataGridView1.Rows.Count - 1; i++)
            {

                for (int j = 2; j < dataGridView1.Columns.Count; j++)
                {

                    try
                    {

                        decimal final_price = decimal.Parse(dataGridView1.Rows[i].Cells[j].Value.ToString()) + (decimal.Parse(dataGridView1.Rows[i].Cells[j].Value.ToString()) * (decimal.Parse(txtExtraCharge.Text) / 100));
                        dataGridView1.Rows[i].Cells[j].ToolTipText = Convert.ToInt32(final_price).ToString();

                    }
                    catch (Exception err)
                    {
                        MessageBox.Show(err.Message);
                    }
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;



            string connectionString = ConfigurationManager.ConnectionStrings["Conn_Str"].ConnectionString;

            using (SqlConnection connection = new SqlConnection())
            {
                connection.ConnectionString = connectionString;

                connection.Open();

                int tot_records = (dataGridView1.Rows.Count - 2) * (dataGridView1.Columns.Count - 2);
                int curr_record = 0;


                //shipping method 먼저 정리
                string sql = "delete from [ShippingByWeightByTotalRecord] where [ShippingMethodId] =" + txtShippingMethod.Text;
                SqlCommand cmd1 = new SqlCommand(sql, connection);
                cmd1.CommandType = CommandType.Text;
                cmd1.ExecuteNonQuery();

                //국가번호
                DataGridViewRow dr_country = dataGridView1.Rows[0];

                for (int i = 1; i < dataGridView1.Rows.Count - 1; i++)
                {
                    if (worker.CancellationPending == true)
                    {
                        e.Cancel = true;
                        return;
                    }
                    else
                    {

                        for (int j = 2; j < dataGridView1.Columns.Count; j++)
                        {
                            curr_record++;

                            //금액이 비어 있으면 배송불가, skip
                            if (dataGridView1.Rows[i].Cells[j].Value.ToString() == "")
                                continue;

                            try
                            {
                                SqlCommand cmd = new SqlCommand("usp_InsertShippingRateOfCountry", connection);
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.Add("@min_weight", SqlDbType.Int).Value = int.Parse(dataGridView1.Rows[i].Cells[0].Value.ToString());
                                cmd.Parameters.Add("@max_weight", SqlDbType.Int).Value = int.Parse(dataGridView1.Rows[i].Cells[1].Value.ToString());
                                cmd.Parameters.Add("@min_cartSubtotal", SqlDbType.Money).Value = int.Parse(txtMinCartSubtotal.Text);
                                cmd.Parameters.Add("@max_cartSubtotal", SqlDbType.Money).Value = int.Parse(txtMaxCartSubtotal.Text);
                                cmd.Parameters.Add("@country", SqlDbType.Int).Value = int.Parse(dr_country.Cells[j].Value.ToString());

                                decimal final_price = decimal.Parse(dataGridView1.Rows[i].Cells[j].Value.ToString()) + (decimal.Parse(dataGridView1.Rows[i].Cells[j].Value.ToString()) * (decimal.Parse(txtExtraCharge.Text) / 100));
                                cmd.Parameters.Add("@additional_price", SqlDbType.Money).Value = final_price;
                                cmd.Parameters.Add("@shipping_method", SqlDbType.Int).Value = int.Parse(txtShippingMethod.Text);

                                cmd.ExecuteNonQuery();
                            }
                            catch (Exception err)
                            {
                                MessageBox.Show(err.Message);
                            }
                        }

                        worker.ReportProgress((int)((float)curr_record / (float)tot_records * 100));
                    }
                }

            }

        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Completed");
            btnSave.Enabled = true;
            btnCencel.Enabled = false;
        }

        private void btnCencel_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
            btnSave.Enabled = true;
            btnCencel.Enabled = false;
        }

        private void FormShippingPrice_Resize(object sender, EventArgs e)
        {
            dataGridView1.Width = this.Width - 80;
            dataGridView1.Height = this.Height - 180;

            btnSave.Top = this.Height - 65;
            btnCencel.Top = this.Height - 65;
            progressBar1.Top = this.Height - 65;
        }
    }
}
