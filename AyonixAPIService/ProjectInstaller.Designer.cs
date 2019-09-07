namespace FoxBIT.Ayonix
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region コンポーネント デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.ayonixAPIServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.ayonixAPIService = new System.ServiceProcess.ServiceInstaller();
            // 
            // ayonixAPIServiceProcessInstaller
            // 
            this.ayonixAPIServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.ayonixAPIServiceProcessInstaller.Password = null;
            this.ayonixAPIServiceProcessInstaller.Username = null;
            // 
            // ayonixAPIService
            // 
            this.ayonixAPIService.Description = "Ayonix API 提供サービス";
            this.ayonixAPIService.DisplayName = "Ayonix API Service";
            this.ayonixAPIService.ServiceName = "AyonixAPIService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ayonixAPIServiceProcessInstaller,
            this.ayonixAPIService});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller ayonixAPIServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller ayonixAPIService;
    }
}