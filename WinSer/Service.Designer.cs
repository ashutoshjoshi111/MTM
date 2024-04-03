namespace WinSer
{
    partial class Service
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            this.tmrRunForPendingJobs = new System.Timers.Timer();
            ((System.ComponentModel.ISupportInitialize)(this.tmrRunForPendingJobs)).BeginInit();
            // 
            // tmrRunForPendingJobs
            // 
            this.tmrRunForPendingJobs.AutoReset = false;
            this.tmrRunForPendingJobs.Interval = 60000D;
            this.tmrRunForPendingJobs.Elapsed += new System.Timers.ElapsedEventHandler(this.tmrRunForPendingJobs_Elapsed);
            // 
            // Service
            // 
            this.ServiceName = "MultiThreadsGenQueue";
            ((System.ComponentModel.ISupportInitialize)(this.tmrRunForPendingJobs)).EndInit();



            this.tmrRunForChunk = new System.Timers.Timer();
            ((System.ComponentModel.ISupportInitialize)(this.tmrRunForChunk)).BeginInit();
            // 
            // for chunk of voice files
            // 
            this.tmrRunForChunk.AutoReset = false;
            this.tmrRunForChunk.Interval = 61000D;
            this.tmrRunForChunk.Elapsed += new System.Timers.ElapsedEventHandler(this.tmrRunForChunk_Elapsed);
            ((System.ComponentModel.ISupportInitialize)(this.tmrRunForChunk)).EndInit();



            this.tmrRunForSentiment = new System.Timers.Timer();
            ((System.ComponentModel.ISupportInitialize)(this.tmrRunForSentiment)).BeginInit();
            // 
            // for sentiment files
            // 
            this.tmrRunForSentiment.AutoReset = false;
            this.tmrRunForSentiment.Interval = 80000D;
            this.tmrRunForSentiment.Elapsed += new System.Timers.ElapsedEventHandler(this.tmrRunForSentiment_Elapsed);
            ((System.ComponentModel.ISupportInitialize)(this.tmrRunForSentiment)).EndInit();
        }

        #endregion

        public System.Timers.Timer tmrRunForPendingJobs;

        public System.Timers.Timer tmrRunForChunk;

        public System.Timers.Timer tmrRunForSentiment;

    }
}
