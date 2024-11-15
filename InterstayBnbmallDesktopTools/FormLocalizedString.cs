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
using System.Configuration;
using Google.Cloud.Translation.V2;

namespace InterstayBnbmallDesktopTools
{
    
    public partial class FormLocalizedString : Form
    {
        string conn_str = ConfigurationManager.ConnectionStrings["Conn_Str"].ConnectionString;
        string srcLanguage = "en";
        string targetLanguage = "ko";

        public FormLocalizedString()
        {            
            InitializeComponent();

            cboSourceLanguage.Items.Add(new LanguageComboboxItem("en", "영어"));
            cboSourceLanguage.Items.Add(new LanguageComboboxItem("ko", "한국어"));
            cboSourceLanguage.Items.Add(new LanguageComboboxItem("zh-CN", "중국어 간체(Simplified)"));
            cboSourceLanguage.Items.Add(new LanguageComboboxItem("zh-TW", "중국어 번체(Traditional)"));
            cboSourceLanguage.Items.Add(new LanguageComboboxItem("ja", "일본어"));

            cboTargetLanguage.Items.Add(new LanguageComboboxItem("en", "영어"));
            cboTargetLanguage.Items.Add(new LanguageComboboxItem("ko", "한국어"));
            cboTargetLanguage.Items.Add(new LanguageComboboxItem("zh-CN", "중국어 간체(Simplified)"));
            cboTargetLanguage.Items.Add(new LanguageComboboxItem("zh-TW", "중국어 번체(Traditional)"));
            cboTargetLanguage.Items.Add(new LanguageComboboxItem("ja", "일본어"));
        }
        private void FormLocalizedString_Resize(object sender, EventArgs e)
        {
            dataGridView1.Width = this.Width - 50;
            dataGridView1.Height = this.Height - 170;

            btnSaveAsExcel.Top = this.Height - 75;
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

                    for(int i=0; i < dataGridView1.Columns.Count; i++)
                    {
                        dataGridView1.Columns[i].Width = (dataGridView1.Width / dataGridView1.Columns.Count) - 20;
                    }
                }

            }
        }

        private bool doValidation()
        {
            if (dataGridView1.Rows.Count == 0)
                return false;

            if (cboSourceLanguage.SelectedIndex < 0 || cboTargetLanguage.SelectedIndex < 0)   {
                MessageBox.Show("원본과 대상 언어를 선택해 주세요");
                return false;
            }

            if (int.Parse(txtSourceIndex.Text) > (dataGridView1.Columns.Count - 1))
            {
                MessageBox.Show("원본의 인덱스 값이 잘못되었습니다. 첫번째 열의 인덱스 값은 0 입니다.");
                return false;
            }

            if (int.Parse(txtTargetIndex.Text) > (dataGridView1.Columns.Count - 1))
            {
                MessageBox.Show("대상의 인덱스 값이 잘못되었습니다. 첫번째 열의 인덱스 값은 0 입니다.");
                return false;
            }

            if (int.Parse(txtSourceIndex.Text) == int.Parse(txtTargetIndex.Text))
            {
                MessageBox.Show("원본과 대상의 인덱스 값이 같습니다. 대상 인덱스 값을 다른 값으로 입력하세요.");
                return false;
            }

            return true;
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!doValidation())
                return;

            if (backgroundWorker1.IsBusy != true)
            {
                // Start the asynchronous operation.
                btnStart.Enabled = false;
                btnCancel.Enabled = true;

                progressBar1.Value = 0;

                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            int tot_records = (dataGridView1.Rows.Count);
            int srcIndex = int.Parse(txtSourceIndex.Text);
            
            int targetIndex = int.Parse(txtTargetIndex.Text);
            TranslationClient client = TranslationClient.Create();

            for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
            {
                if(worker.CancellationPending == true) {
                    e.Cancel = true;
                    return;
                }
                else  {

                    try
                    {
                        string srcText = dataGridView1.Rows[i].Cells[srcIndex].Value.ToString();

                        //HTML 타입인 경우 별도의 메서드 호출
                        if (chkHTML.Checked == true)
                        {
                            var response = client.TranslateHtml(srcText, targetLanguage, srcLanguage);
                            dataGridView1.Rows[i].Cells[targetIndex].Value = response.TranslatedText;
                        }
                        else
                        {
                            var response = client.TranslateText(srcText, targetLanguage, srcLanguage);
                            dataGridView1.Rows[i].Cells[targetIndex].Value = response.TranslatedText;
                        }
                        
                    }
                    catch(Exception err)
                    {
                        dataGridView1.Rows[i].Cells[targetIndex].Value = err.Message;
                    }

                    worker.ReportProgress((int)((float)(i+1) / (float)tot_records * 100));
                }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Value = progressBar1.Maximum;
            btnStart.Enabled = true;
            btnCancel.Enabled = false;

            MessageBox.Show("Completed");            
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
            btnStart.Enabled = true;
            btnCancel.Enabled = false;
        }

        private void cboSourceLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            srcLanguage = ((LanguageComboboxItem)cboSourceLanguage.SelectedItem).Value;
        }

        private void cboTargetLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            targetLanguage = ((LanguageComboboxItem)cboTargetLanguage.SelectedItem).Value;
        }

        private void btnSaveAsExcel_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog1 = new SaveFileDialog())
            {
                saveFileDialog1.InitialDirectory = "c:\\";
                saveFileDialog1.Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
                saveFileDialog1.FilterIndex = 1;
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    Workbook workbook = new Workbook();
                    Worksheet sheet = workbook.Worksheets[0];
                    DataTable dt = (DataTable)dataGridView1.DataSource;
                    sheet.InsertDataTable(dt, true, 1, 1);
                    
                    workbook.SaveToFile(saveFileDialog1.FileName, ExcelVersion.Version2013);
                }
            }

        }
    }
}
