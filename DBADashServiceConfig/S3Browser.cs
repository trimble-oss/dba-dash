using DBADash;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashServiceConfig
{
    public partial class S3Browser : Form
    {
        public S3Browser()
        {
            InitializeComponent();
        }

        public string AccessKey;
        public string SecretKey;
        public string AWSProfile;
        public string Folder
        {
            get
            {
                return txtFolder.Text;
            }
            set
            {
                txtFolder.Text = value;
            }
        }

        public string AWSURL { 
            get {
                if (!string.IsNullOrEmpty(cboBuckets.Text) & !String.IsNullOrEmpty(txtFolder.Text))
                {
                    string folder = txtFolder.Text.Trim(new char[] { ' ', '/'});
                   
                    return string.Format("https://{0}.s3.amazonaws.com/{1}", cboBuckets.Text, folder);
                }
                else
                {
                    return string.Empty;
                }
            } 
        }

        private void addBuckets()
        {
            var cred = DBADash.AWSTools.GetCredentials(AWSProfile, AccessKey, SecretKey);
            using (var s3 = new Amazon.S3.AmazonS3Client(cred))
            {
                using (var listBucketsTask = s3.ListBucketsAsync())
                {
                    listBucketsTask.Wait();

                    foreach (var b in listBucketsTask.Result.Buckets)
                    {
                        cboBuckets.Items.Add(b.BucketName);
                    }
                }
            }
        }


        private void cboBuckets_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void cboBuckets_DropDown(object sender, EventArgs e)
        {
            if (cboBuckets.Items.Count == 0)
            {
                try
                {
                    addBuckets();
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error listing buckets.  If you don't have list bucket access, enter the name of the bucket manually." +  ex.Message, "List Bucket Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void bttnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cboBuckets.Text))
            {
                MessageBox.Show("Please select a bucket", "Bucket required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
                this.DialogResult = DialogResult.OK;
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
