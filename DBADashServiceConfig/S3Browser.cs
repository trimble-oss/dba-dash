using DBADashGUI.Theme;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashServiceConfig
{
    public partial class S3Browser : Form
    {
        public S3Browser()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public string AccessKey;
        public string SecretKey;
        public string AWSProfile;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Folder
        {
            get => txtFolder.Text; set => txtFolder.Text = value;
        }

        public string AWSURL
        {
            get
            {
                if (!string.IsNullOrEmpty(cboBuckets.Text) & !String.IsNullOrEmpty(txtFolder.Text))
                {
                    var folder = txtFolder.Text.Trim(' ', '/');

                    return $"https://{cboBuckets.Text}.s3.amazonaws.com/{folder}";
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        private async Task AddBuckets()
        {
            var cred = DBADash.AWSTools.GetCredentials(AWSProfile, AccessKey, SecretKey);
            using var s3 = new Amazon.S3.AmazonS3Client(cred);
            var result = await s3.ListBucketsAsync();

            foreach (var b in result.Buckets)
            {
                cboBuckets.Items.Add(b.BucketName);
            }
        }

        private async void CboBuckets_DropDown(object sender, EventArgs e)
        {
            if (cboBuckets.Items.Count != 0) return;
            try
            {
                await AddBuckets();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error listing buckets.  If you don't have list bucket access, enter the name of the bucket manually.", "List Bucket Error");
            }
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cboBuckets.Text))
            {
                MessageBox.Show("Please select a bucket", "Bucket required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult = DialogResult.OK;
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}