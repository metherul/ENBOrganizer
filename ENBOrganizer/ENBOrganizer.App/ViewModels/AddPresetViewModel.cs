﻿using ENBOrganizer.App.Messages;
using ENBOrganizer.Domain;
using ENBOrganizer.Domain.Entities;
using ENBOrganizer.Domain.Services;
using ENBOrganizer.Util;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace ENBOrganizer.App.ViewModels
{
    public class AddPresetViewModel : ViewModelBase
    {
        private readonly PresetService _presetService;
        private readonly FileSystemService<Binary> _binaryService;
        private readonly DialogService _dialogService;
        
        private string _name;

        public string Name
        {
            get { return _name; }
            set { Set(nameof(Name), ref _name, value.Trim()); }
        }

        private string _sourcePath;

        public string SourcePath
        {
            get { return _sourcePath; }
            set { Set(nameof(SourcePath), ref _sourcePath, value.Trim()); }
        }

        public Binary Binary { get; set; }
        public Game CurrentGame { get { return Properties.Settings.Default.CurrentGame; } }
        public ObservableCollection<Binary> Binaries { get; set; }
        public ICommand BrowseForDirectoryCommand { get; set; }
        public ICommand BrowseForArchiveCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand SaveCommand { get; set; }

        public AddPresetViewModel(PresetService presetService, FileSystemService<Binary> binaryService, DialogService dialogService)
        {
            _presetService = presetService;
            _binaryService = binaryService;
            _binaryService.ItemsChanged += _binaryService_ItemsChanged;

            _dialogService = dialogService;

            BrowseForDirectoryCommand = new RelayCommand(BrowseForDirectory);
            BrowseForArchiveCommand = new RelayCommand(BrowseForArchive);
            CancelCommand = new RelayCommand(Close);
            SaveCommand = new RelayCommand(Save, CanSave);

            Binaries = _binaryService.GetAll().ToObservableCollection();
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(SourcePath)
                && (Directory.Exists(SourcePath) || File.Exists(SourcePath));
        }

        private void _binaryService_ItemsChanged(object sender, RepositoryChangedEventArgs repositoryChangedEventArgs)
        {
            if (repositoryChangedEventArgs.RepositoryActionType == RepositoryActionType.Added)
                Binaries.Add(repositoryChangedEventArgs.Entity as Binary);
            else
                Binaries.Remove(repositoryChangedEventArgs.Entity as Binary);
        }

        private void Save()
        {
            Preset preset = new Preset(Name, CurrentGame);

            // Detect whether the user has selected the default value in the ComboBox.
            if (Binary.Name != "-- None --" && Binary.Game != null)
                preset.Binary = Binary;

            try
            {
                _presetService.Import(preset, SourcePath);
            }
            catch (Exception exception)
            {
                _dialogService.ShowErrorDialog(exception.Message);
            }
            finally
            {
                Name = string.Empty;
                SourcePath = string.Empty;

                Close();
            }
        }

        private void BrowseForDirectory()
        {
            string directoryPath = _dialogService.PromptForFolder("Please select the preset folder...");

            if (string.IsNullOrWhiteSpace(directoryPath))
                return;

            SourcePath = directoryPath;
            Name = new DirectoryInfo(directoryPath).Name;
        }

        private void BrowseForArchive()
        {
            string archivePath = _dialogService.PromptForFile("Please select an archive file", "ZIP Files(*.zip) | *.zip");

            if (string.IsNullOrWhiteSpace(archivePath))
                return;

            SourcePath = archivePath;
            Name = Path.GetFileNameWithoutExtension(SourcePath);
        }

        private void Close()
        {
            Name = string.Empty;
            SourcePath = string.Empty;

            _dialogService.CloseDialog(DialogName.AddPreset);
        }
    }
}
