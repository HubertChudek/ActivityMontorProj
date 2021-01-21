﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using MessageBox = System.Windows.MessageBox;
using ExToolkit  = Xceed.Wpf.Toolkit;

namespace ActivityMonitor.Forms
{
    /// <summary>
    /// Logika interakcji dla klasy FormActivityWindow.xaml
    /// </summary>
    public partial class FormMealWindow : Window
    {
        //obiekt zarządzający bazą dancyh
        private DatabaseManager dbManager = new DatabaseManager();
        //identyfikator wiersza z bazy danych, 0 - nowe zdarzenie, <0 - zdarzenie pobrane z bazy
        private int AppId = 0;

        public FormMealWindow()
        {
            InitializeComponent();
            InitializeControlsValues();
        }

        public FormMealWindow(int id)
        {
            InitializeComponent();
            AppId = id;
            InitializeControlsValues();
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            var nutri = new NutritionixControl();
            Nutritionix.Item foo = nutri.LookupNutritionInfo(txtName.Text);

            PopulateFormFields(foo);

            //string toPrint = $"Product name: {foo.Name}, KCal: {foo.NutritionFact_Calories.ToString()}\n"; 
            //Debug.Print(toPrint);
        }

        //metoda zapełnia pola formularza danymi pobranymi z API
        private void PopulateFormFields(Nutritionix.Item item)
        {
            txtName.Text = item.Name;
            txtCalories.Text = Math.Floor((decimal)item.NutritionFact_Calories).ToString();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            ShowConnectionResult();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CheckIfInputCorrect())
            {
                return;
            }

            MessageBoxResult confirmResult = ExToolkit.MessageBox.Show("Are you sure to save?",
                "Please confirm.",
                MessageBoxButton.YesNo);
            if (confirmResult != MessageBoxResult.Yes)
            {
                return;
            }

            if (AppId == 0)
            {
                InsertActivity();
            }
            else
            {
                UpdateActivity();
            }
        }

        //metoda aktualizująca informacje z formularza
        private void UpdateActivity()
        {
            string sql =
                $"update meal set AppDate = '{dtpDate.SelectedDate.Value.Date.ToShortDateString()}'," +
                $"StartTime = '{tpStartTime.Value}'," +
                $"EndTime = '{tpEndTime.Value}'," +
                $"Type = '{cbxType.Text}'," +
                $"Calories = '{int.Parse(txtCalories.Text)}'," +
                $"MealName = '{txtName.Text}'," +
                $"Quantity = '{int.Parse(txtQuantity.Text)}'," +
                $"Unit = '{cbxUnit.Text}'" +
                $" where ID = {AppId}";
            bool updateStatus = dbManager.InsertUpdateDelete(sql);
            NotifyOperationStatus(updateStatus);
        }

        //metoda wprowadza do bazy informacje z formularza
        private void InsertActivity()
        {
            string sql =
            $"insert into meal(AppDate, StartTime, EndTime, Calories, Type, MealName, Quantity, Unit) " +
            $"values('{dtpDate.SelectedDate.Value.Date.ToShortDateString()}', '{tpStartTime.Value}', " +
            $"'{tpEndTime.Value}', '{txtCalories.Text}', '{cbxType.Text}', '{txtName.Text}', " +
            $"'{txtQuantity.Text}', '{cbxUnit.Text}')";
            bool instertStatus = dbManager.InsertUpdateDelete(sql);
            NotifyOperationStatus(instertStatus);
        }

        //usuwa wiersz z bazy danych wg podanego ID
        private void DeleteActivity()
        {
            string sql =
                $"delete from meal where ID = {AppId}";
            bool daleteStatus = dbManager.InsertUpdateDelete(sql);
            NotifyOperationStatus(daleteStatus);
            Close();
        }

        //okienko powiadomienia czy operacja się udała
        private void NotifyOperationStatus(bool status)
        {
            if (status)
            {
                ExToolkit.MessageBox.Show("Operation successful");
            }
            else
            {
                ExToolkit.MessageBox.Show("Operation failed");
            }
        }

        //metoda sprawdza czy podane dane są poprawne
        private bool CheckIfInputCorrect()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ExToolkit.MessageBox.Show("Product name type must be specified.");
                return true;
            }

            return false;
        }

        //wyświetlanie okienek ze stanem połaczenia na potrzeby testów
        private void ShowConnectionResult()
        {
            try
            {
                dbManager.Connect();
                MessageBox.Show("Connected");
            }
            catch (Exception)
            {
                MessageBox.Show("Error");
            }
        }

        //metoda inicjalizuje wartości kontrolek domyślną wartością
        private void InitializeControlsValues()
        {
            dtpDate.SelectedDate = DateTime.Today;
            tpStartTime.Value = DateTime.Now;
            tpEndTime.Value = DateTime.Now.AddHours(1);
            if (AppId == 0)
            {
                btnDelete.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtCalories_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            AllowOnlyDigitsAtInput(e);
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            AllowOnlyDigitsAtInput(e);
        }

        //metoda sprawdza znaki na wejsciu i pozwala wpisać tylko cyfry
        private static void AllowOnlyDigitsAtInput(TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            dbManager.cn.Close();
            this.Close();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmResult = ExToolkit.MessageBox.Show("Are you sure to delete?",
                "Please confirm.",
                MessageBoxButton.YesNo);
            if (confirmResult != MessageBoxResult.Yes)
            {
                return;
            }
            DeleteActivity();
        }

        public void FillFieldsFromDataRow(DataRow dataRow)
        {
            this.AppId = Int32.Parse(dataRow["ID"].ToString());
            this.dtpDate.SelectedDate = DateTime.Parse(dataRow["AppDate"].ToString());
            this.tpStartTime.Value = DateTime.Parse(dataRow["StartTime"].ToString());
            this.tpEndTime.Value = DateTime.Parse(dataRow["EndTime"].ToString());
            this.cbxType.Text = dataRow["Type"].ToString();
            this.txtCalories.Text = dataRow["Calories"].ToString();
            this.txtName.Text = dataRow["MealName"].ToString();
            this.txtQuantity.Text = dataRow["Quantity"].ToString();
            this.cbxUnit.Text = dataRow["Unit"].ToString();
        }
    }
}
