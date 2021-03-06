﻿using System;
using System.Threading;
using System.Collections.Generic;

using Android.App;
using Android.Views;
using Android.Widget;
using Android.Content;
using Android.Views.InputMethods;
using Android.InputMethodServices;

using LocalBox_Common;

using Xamarin;
using System.Net;

namespace LocalBox_Droid
{
	public class DialogHelperForHomeActivity
	{
		public HomeActivity homeActivity;

		public DialogHelperForHomeActivity (HomeActivity homeActivity)
		{
			this.homeActivity = homeActivity;
		}



		public void ShowCertificateMismatchDialog()
		{
			var builder = new AlertDialog.Builder(homeActivity)
				.SetTitle("Waarschuwing")
				.SetMessage("De identiteit van de server kan niet goed vastgesteld worden. " +
					"Maakt u gebruik van een vertrouwd netwerk om de identiteit " +
					"extra te controleren?")
				.SetPositiveButton("Ja", async (s, args) =>
					{
						SslValidator.CertificateErrorRaised = false;

						//Get new certificate from server
						bool newCertificateIsValid = CertificateHelper.RenewCertificateForLocalBox(DataLayer.Instance.GetSelectedOrDefaultBox());

						if(newCertificateIsValid){
							homeActivity.ShowToast ("Controle met succes uitgevoerd. U kunt weer verder werken.");
						}else {
							homeActivity.ShowToast ("Dit netwerk is niet te vertrouwen.");
						}
					})
				.SetNegativeButton("Nee", (s, args) =>{ });
			builder.Show ();
		}



		public void ShowShareFileDatePicker (string pathToNewFileShare)
		{
			homeActivity.HideProgressDialog ();

			var selectedExpirationDate = DateTime.Now.AddDays (7);//Selectie standaard op 7 dagen na vandaag
			var picker = new DatePickerDialog (homeActivity, (object sender, DatePickerDialog.DateSetEventArgs e) => {
				selectedExpirationDate = e.Date;

				if (selectedExpirationDate.Date <= DateTime.Now.Date) {
					homeActivity.HideProgressDialog ();
					Toast.MakeText (Android.App.Application.Context, "Gekozen vervaldatum moet na vandaag zijn. Probeer het a.u.b. opnieuw.", ToastLength.Long).Show ();
				} else {
					ThreadPool.QueueUserWorkItem (async o => {
						PublicUrl createdPublicUrl = await DataLayer.Instance.CreatePublicFileShare (pathToNewFileShare, selectedExpirationDate);

						if (createdPublicUrl != null) {
							//Open e-mail intent
							var emailIntent = new Intent (Android.Content.Intent.ActionSend);

							string bodyText = "Mijn gedeelde bestand: \n" +
								createdPublicUrl.publicUri + "\n \n" +
								"Deze link is geldig tot: " + selectedExpirationDate.ToString ("dd-MM-yyyy");

							emailIntent.PutExtra (Android.Content.Intent.ExtraSubject, "Publieke URL naar gedeeld LocalBox bestand");
							emailIntent.PutExtra (Android.Content.Intent.ExtraText, bodyText);
							emailIntent.SetType ("message/rfc822");
							homeActivity.RunOnUiThread (() => {
								homeActivity.HideProgressDialog ();

								homeActivity.StartActivity (emailIntent);
							});
						} else {
							homeActivity.HideProgressDialog ();
							Toast.MakeText (Android.App.Application.Context, "Bestand delen mislukt", ToastLength.Short).Show ();
						}
					});
				}


			}, selectedExpirationDate.Year, selectedExpirationDate.Month - 1, selectedExpirationDate.Day);
			picker.SetTitle ("Selecteer vervaldatum:");
			picker.Show ();
		}
			
		public void ShowLoginDialog (string pleioUrl)
		{
			Android.App.FragmentTransaction fragmentTransaction;
			fragmentTransaction = homeActivity.FragmentManager.BeginTransaction ();

			Android.App.DialogFragment dialogFragmentLogin;
			LoginFragment loginFragment = new LoginFragment (pleioUrl, homeActivity);

			homeActivity.dialogLogin = loginFragment;
			homeActivity.dialogLogin.Show (fragmentTransaction, "logindialog");
		}

		public void ShowAddSitesDialog()
		{	
			try {
				Android.App.FragmentTransaction fragmentTransaction;
				fragmentTransaction = homeActivity.FragmentManager.BeginTransaction ();

				Android.App.DialogFragment dialogFragmentAddSites;
				AddSitesFragment addSitesFragment = new AddSitesFragment ();

				homeActivity.dialogFragmentAddSites = addSitesFragment;
				homeActivity.dialogFragmentAddSites.Show (fragmentTransaction, "addsitesdialog");
			} catch (Exception ex) {
				Insights.Report (ex);
			}
		}
			
		public void ShowAboutAppDialog ()
		{
			Android.App.FragmentTransaction fragmentTransaction;
			fragmentTransaction = homeActivity.FragmentManager.BeginTransaction ();

			Android.App.DialogFragment dialogFragmentAboutApp;
			AboutAppFragment aboutFragment = new AboutAppFragment ();

			dialogFragmentAboutApp = aboutFragment;
			dialogFragmentAboutApp.Show (fragmentTransaction, "aboutdialog");
		}

		public async void ShowMoveFileDialog(TreeNode treeNodeToMove)
		{
			Console.WriteLine ("Bestand om te verplaatsen: " + treeNodeToMove.Name);
			homeActivity.ShowProgressDialog (null);
			try {
				Android.App.FragmentTransaction fragmentTransaction;
				fragmentTransaction = homeActivity.FragmentManager.BeginTransaction ();

				List<TreeNode>foundDirectoryTreeNodes 	= new List<TreeNode>();
				TreeNode rootTreeNode = await DataLayer.Instance.GetFolder ("/");

				foreach(TreeNode foundTreeNode in rootTreeNode.Children)
				{
					if(foundTreeNode.IsDirectory)
					{
						foundDirectoryTreeNodes.Add(foundTreeNode);
					}
				}
				MoveFileFragment moveFileFragment = new MoveFileFragment(foundDirectoryTreeNodes, treeNodeToMove, homeActivity);
				homeActivity.dialogFragmentMoveFile = moveFileFragment;

				homeActivity.HideProgressDialog ();

				if (foundDirectoryTreeNodes.Count > 0) {
					homeActivity.dialogFragmentMoveFile.Show (fragmentTransaction, "movefiledialog");
				} else {
					homeActivity.ShowToast("Geen mappen gevonden om bestand naar te verplaatsen");
				}
			} catch (Exception ex){
				Insights.Report(ex);
				homeActivity.HideProgressDialog ();
				homeActivity.ShowToast ("Er is iets fout gegaan bij het ophalen van mappen. \nProbeer het a.u.b. opnieuw");
			}
		}

		public void HideMoveFileDialog ()
		{
			homeActivity.dialogFragmentMoveFile.Dismiss ();
			homeActivity.ShowToast("Bestand succesvol verplaatst");
			homeActivity.RefreshExplorerFragmentData ();
		}

		public void HideAddSitesDialog ()
		{
			homeActivity.dialogFragmentAddSites.Dismiss ();
		}

		public void HideLoginDialog()
		{
			homeActivity.dialogLogin.Dismiss ();
		}

		public void ShowNewFolderDialog ()
		{
			LayoutInflater factory = LayoutInflateHelper.GetLayoutInflater (homeActivity);

			View viewNewFolder = factory.Inflate (Resource.Layout.dialog_new_folder, null);

			EditText editTextFolderName = (EditText)viewNewFolder.FindViewById<EditText> (Resource.Id.editText_dialog_folder_name);          

			//Build the dialog
			var dialogBuilder = new AlertDialog.Builder (homeActivity);
			dialogBuilder.SetTitle (Resource.String.folder_new);
			dialogBuilder.SetView (viewNewFolder);
			dialogBuilder.SetPositiveButton (Resource.String.add, (EventHandler<DialogClickEventArgs>)null);
			dialogBuilder.SetNegativeButton (Resource.String.cancel, (EventHandler<DialogClickEventArgs>)null);

			var dialog = dialogBuilder.Create ();
			dialog.Show ();

			//Get the buttons
			var buttonAddFolder = dialog.GetButton ((int)DialogButtonType.Positive);
			var buttonCancel = dialog.GetButton ((int)DialogButtonType.Negative);

			buttonAddFolder.Click += async(sender, args) => {
				if (String.IsNullOrEmpty (editTextFolderName.Text)) {
					homeActivity.ShowToast("Naam is niet ingevuld");
				} else {
					homeActivity.ShowProgressDialog("Map wordt aangemaakt. Een ogenblik geduld a.u.b.");
					try{
						int numberOfDirectoriesOpened = ExplorerFragment.openedDirectories.Count;
						string directoryNameToUploadFileTo = ExplorerFragment.openedDirectories [numberOfDirectoriesOpened - 1];

						bool addedSuccesfully = (await DataLayer.Instance.CreateFolder (System.IO.Path.Combine (directoryNameToUploadFileTo, (editTextFolderName.Text))));

						dialog.Dismiss ();

						if (!addedSuccesfully) {
							homeActivity.HideProgressDialog();
							homeActivity.ShowToast("Toevoegen map mislukt. Probeer het a.u.b. opnieuw");
						} else {
							homeActivity.HideProgressDialog();
							homeActivity.ShowToast("Map succesvol toegevoegd");

							//Refresh data
							homeActivity.RefreshExplorerFragmentData();
						}
					}catch (Exception ex){
						Insights.Report(ex);
						homeActivity.HideProgressDialog();
						homeActivity.ShowToast("Toevoegen map mislukt. Probeer het a.u.b. opnieuw");
					}
				}
			};
			buttonCancel.Click += (sender, args) => {
				dialog.Dismiss ();
			};
		}

		public void ShowHttpWarningDialog(string urlToOpen, AlertDialog urlDialog)
		{
			var dialogAlert = (new AlertDialog.Builder (homeActivity)).Create ();
			dialogAlert.SetIcon (Android.Resource.Drawable.IcDialogAlert);
			dialogAlert.SetTitle ("Waarschuwing");
			dialogAlert.SetMessage ("U heeft een http webadres opgegeven. Weet u zeker dat u een onbeveiligde verbinding wilt opzetten?");
			dialogAlert.SetButton2 ("Cancel", (s, ev) => { dialogAlert.Dismiss(); });
			dialogAlert.SetButton ("Ga verder", (s, ev) => {
				urlDialog.Dismiss ();
				dialogAlert.Dismiss();
			});
			dialogAlert.Show ();
		}


		public void ShowSelfSignedCertificateDialog(string urlToOpen, AlertDialog urlDialog)
		{
			var dialogAlert = (new AlertDialog.Builder (homeActivity)).Create ();
			dialogAlert.SetIcon (Android.Resource.Drawable.IcDialogAlert);
			dialogAlert.SetTitle ("Waarschuwing");
			dialogAlert.SetMessage ("U heeft een webadres opgegeven met een ssl certificaat welke niet geverifieerd is. Weet u zeker dat u wilt doorgaan?");
			dialogAlert.SetButton2 ("Cancel", (s, ev) => { dialogAlert.Dismiss(); });
			dialogAlert.SetButton ("Ga verder", (s, ev) => {
				urlDialog.Dismiss ();
				dialogAlert.Dismiss();
			});
			dialogAlert.Show ();
		}


		public void ShowInvalidCertificateDialog(string urlToOpen, AlertDialog urlDialog)
		{
			var dialogAlert = (new AlertDialog.Builder (homeActivity)).Create ();
			dialogAlert.SetIcon (Android.Resource.Drawable.IcDialogAlert);
			dialogAlert.SetTitle ("Error");
			dialogAlert.SetMessage ("U heeft een webadres opgegeven met een ongeldig SSL certificaat. U kunt hierdoor geen verbinding maken met de betreffende LocalBox");
			dialogAlert.SetButton ("OK", (s, ev) => {
				dialogAlert.Dismiss();
			});
			dialogAlert.Show ();
		}

		public void ShowErrorRegisterDialog(string urlToOpen, AlertDialog urlDialog)
		{
			var dialogAlert = (new AlertDialog.Builder (homeActivity)).Create ();
			dialogAlert.SetIcon (Android.Resource.Drawable.IcDialogAlert);
			dialogAlert.SetTitle ("Error");
			dialogAlert.SetMessage ("Er is een fout opgetreden. Controleer de verbinding en webadres en probeer het a.u.b. nogmaals");
			dialogAlert.SetButton ("OK", (s, ev) => {
				dialogAlert.Dismiss();
			});
			dialogAlert.Show ();
		}

		public void ShowErrorLoginDialog()
		{
			var dialogAlert = (new AlertDialog.Builder (homeActivity)).Create ();
			dialogAlert.SetIcon (Android.Resource.Drawable.IcDialogAlert);
			dialogAlert.SetTitle ("Error");
			dialogAlert.SetMessage ("Kan niet inloggen met de opgegeven gebruikersnaam en wachtwoord. Controleer de gegevens en probeer het a.u.b. nogmaals");
			dialogAlert.SetButton ("OK", (s, ev) => {
				dialogAlert.Dismiss();
			});
			dialogAlert.Show ();
		}

		//Register new LocalBox part 3
		public async void AddLocalBox(LocalBox lbToAdd)
		{
			bool result = false;

			homeActivity.ShowProgressDialog ("LocalBox laden...");

			result = await BusinessLayer.Instance.Authenticate (lbToAdd);

			homeActivity.HideProgressDialog ();
		}

		//Register new LocalBox part 4
		public void SetUpPassphrase (LocalBox localBox)
		{
			LayoutInflater factory = LayoutInflateHelper.GetLayoutInflater (homeActivity);
			View viewNewPhrase = factory.Inflate (Resource.Layout.dialog_new_passphrase, null);

			EditText editNewPassphrase = (EditText)viewNewPhrase.FindViewById<EditText> (Resource.Id.editText_dialog_new_passphrase);          
			EditText editNewPassphraseVerify = (EditText)viewNewPhrase.FindViewById<EditText> (Resource.Id.editText_dialog_new_passphrase_verify);

			var dialogBuilder = new AlertDialog.Builder (homeActivity);
			dialogBuilder.SetTitle ("Passphrase");
			dialogBuilder.SetView (viewNewPhrase);
			dialogBuilder.SetPositiveButton ("OK", (EventHandler<DialogClickEventArgs>)null);
			dialogBuilder.SetNegativeButton (Resource.String.cancel, (EventHandler<DialogClickEventArgs>)null);
			var dialog = dialogBuilder.Create ();
			dialog.Show ();

			var buttonCancel = dialog.GetButton ((int)DialogButtonType.Negative);
			var buttonAddPassphrase = dialog.GetButton ((int)DialogButtonType.Positive);
			buttonAddPassphrase.Click += async (sender, args) => {
				string passphraseOne = editNewPassphrase.Text;
				string passphraseTwo = editNewPassphraseVerify.Text;

				if (String.IsNullOrEmpty (passphraseOne)) {
					homeActivity.ShowToast("Passphrase is niet ingevuld");
				} else if (String.IsNullOrEmpty (passphraseTwo)) {
					homeActivity.ShowToast("U dient uw ingevoerde passphrase te verifieren");
				} else {
					if (!passphraseOne.Equals (passphraseTwo)) {
						homeActivity.ShowToast("De ingevoerde passphrases komen niet overeen. Corrigeer dit a.u.b.");
					} else {
						try 
						{
							homeActivity.ShowProgressDialog ("Passphrase aanmaken. Dit kan enige tijd in beslag nemen. Een ogenblik geduld a.u.b.");
							bool newPassphraseSucceeded = await BusinessLayer.Instance.SetPublicAndPrivateKey (localBox, passphraseOne);
							homeActivity.HideProgressDialog();

							if (!newPassphraseSucceeded) {
								homeActivity.ShowToast("Passphrase instellen mislukt. Probeer het a.u.b. opnieuw");
							} 
							else {
								dialog.Dismiss ();
								homeActivity.ShowToast("LocalBox succesvol geregistreerd");

								homeActivity.menuFragment.UpdateLocalBoxes ();
								SplashActivity.intentData = null;
							}
						} 
						catch (Exception ex){
							Insights.Report(ex);
							homeActivity.HideProgressDialog ();
							homeActivity.ShowToast("Passphrase instellen mislukt. Probeer het a.u.b. opnieuw");
						}
					}
				}
			};
			buttonCancel.Click += (sender, args) => {
				DataLayer.Instance.DeleteLocalBox (localBox.Id);
				homeActivity.menuFragment.UpdateLocalBoxes ();
				dialog.Dismiss ();
			};
		}


		//Register new LocalBox part 4
		public void EnterPassphrase (LocalBox localBox)
		{
			LayoutInflater factory = LayoutInflateHelper.GetLayoutInflater (homeActivity);
			View viewNewPhrase = factory.Inflate (Resource.Layout.dialog_enter_passphrase, null);
			EditText editEnterPassphrase = (EditText)viewNewPhrase.FindViewById<EditText> (Resource.Id.editText_dialog_enter_passphrase);          

			var dialogBuilder = new AlertDialog.Builder (homeActivity);
			dialogBuilder.SetTitle ("Passphrase");
			dialogBuilder.SetView (viewNewPhrase);
			dialogBuilder.SetPositiveButton ("OK", (EventHandler<DialogClickEventArgs>)null);
			dialogBuilder.SetNegativeButton (Resource.String.cancel, (EventHandler<DialogClickEventArgs>)null);
			var dialog = dialogBuilder.Create ();
			dialog.Show ();

			var buttonCancel = dialog.GetButton ((int)DialogButtonType.Negative);
			var buttonAddPassphrase = dialog.GetButton ((int)DialogButtonType.Positive);
			buttonAddPassphrase.Click += async (sender, args) => {
				string passphrase = editEnterPassphrase.Text;

				if (String.IsNullOrEmpty (passphrase)) {
					homeActivity.ShowToast("Passphrase is niet ingevuld");
				} 
				else {
					try {
						homeActivity.ShowProgressDialog ("Uw passphrase controleren. Een ogenblik geduld a.u.b.");
						bool correctPassphraseEntered = await BusinessLayer.Instance.ValidatePassPhrase (localBox, passphrase);
						homeActivity.HideProgressDialog();

						if (!correctPassphraseEntered) {
							homeActivity.ShowToast("Passphrase onjuist. Probeer het a.u.b. opnieuw");
						} else {
							dialog.Dismiss ();
							homeActivity.ShowToast("Passphrase geaccepteerd en Pleiobox succesvol geregistreerd");
							homeActivity.menuFragment.UpdateLocalBoxes ();
							SplashActivity.intentData = null;
						}
					} catch (Exception ex){
						Insights.Report(ex);
						homeActivity.HideProgressDialog ();
						homeActivity.ShowToast("Passphrase verifieren mislukt. Probeer het a.u.b. opnieuw");
					}
				}
			};
			buttonCancel.Click += (sender, args) => {
				DataLayer.Instance.DeleteLocalBox (localBox.Id);
				homeActivity.menuFragment.UpdateLocalBoxes ();
				dialog.Dismiss ();
			};
		}
	}
}

