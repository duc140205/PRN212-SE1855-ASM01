﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BusinessObjects;
using HoangLeThanhDucWPF.ViewModels;
using Services;


namespace HoangLeThanhDucWPF
{
    /// <summary>
    /// Interaction logic for AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {


        public AdminWindow()
        {
            InitializeComponent();
            var viewModel = new AdminViewModel();
            this.DataContext = viewModel;

            // Đăng ký sự kiện RequestClose
            viewModel.RequestClose += () => {
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            };
        }
    }
}

