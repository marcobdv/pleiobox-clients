// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LocalBox_iOS.Views
{
	[Register ("NodeView")]
	partial class NodeView
	{
		[Outlet]
		UIKit.UIView ButtonContainer { get; set; }

		[Outlet]
		UIKit.UIView kleurenBalk { get; set; }

		[Outlet]
		UIKit.UIButton MaakFolderButton { get; set; }

		[Outlet]
		UIKit.UITableView NodeItemTable { get; set; }

		[Outlet]
		UIKit.UITableViewController NodeItemTableController { get; set; }

		[Outlet]
		UIKit.UIButton SyncButton { get; set; }

		[Outlet]
		UIKit.UIButton TerugButton { get; set; }

		[Outlet]
		UIKit.UIButton UploadFotoButton { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ButtonContainer != null) {
				ButtonContainer.Dispose ();
				ButtonContainer = null;
			}

			if (kleurenBalk != null) {
				kleurenBalk.Dispose ();
				kleurenBalk = null;
			}

			if (MaakFolderButton != null) {
				MaakFolderButton.Dispose ();
				MaakFolderButton = null;
			}

			if (NodeItemTable != null) {
				NodeItemTable.Dispose ();
				NodeItemTable = null;
			}

			if (NodeItemTableController != null) {
				NodeItemTableController.Dispose ();
				NodeItemTableController = null;
			}

			if (SyncButton != null) {
				SyncButton.Dispose ();
				SyncButton = null;
			}

			if (TerugButton != null) {
				TerugButton.Dispose ();
				TerugButton = null;
			}

			if (UploadFotoButton != null) {
				UploadFotoButton.Dispose ();
				UploadFotoButton = null;
			}
		}
	}
}
