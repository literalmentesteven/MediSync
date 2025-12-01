using MediSync.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace MediSync.Views
{
    public partial class SchedulePage : ContentPage
    {
        private DateTime _currentMonth;
        private DateTime _selectedDate;
        private List<Paciente> _allPacientes = new(); 
        private readonly HttpClient _httpClient;

        public SchedulePage()
        {
            InitializeComponent();
            
            var services = Application.Current?.Handler?.MauiContext?.Services;
            _httpClient = services?.GetService<HttpClient>() ?? new HttpClient { BaseAddress = new Uri("http://localhost:7151") };
            
            _currentMonth = DateTime.Now;
            _selectedDate = DateTime.Now.Date;
        }

        private async void OnPageLoaded(object sender, EventArgs e)
        {
            await CargarCitas();
            RenderCalendar();
            FiltrarCitasPorFecha(_selectedDate);
        }

        private async Task CargarCitas()
        {
            try
            {
                var data = await _httpClient.GetFromJsonAsync<List<Paciente>>("api/pacientes");
                if (data != null) _allPacientes = data;
            }
            catch { await DisplayAlert("Error", "No se cargaron las citas", "OK"); }
        }

        private void RenderCalendar()
        {
            if (LblCurrentMonth == null) return;

            LblCurrentMonth.Text = _currentMonth.ToString("MMMM yyyy").ToUpper();
            
            var daysList = new List<CalendarDay>();
            var firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            int offset = (int)firstDayOfMonth.DayOfWeek;

            for (int i = 0; i < offset; i++) daysList.Add(new CalendarDay { DayNumber = "" });

            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(_currentMonth.Year, _currentMonth.Month, d);
                bool isSelected = date.Date == _selectedDate.Date;
                bool isToday = date.Date == DateTime.Now.Date;

                Color bg = Colors.Transparent;
                Color txt = Color.FromArgb("#333333"); 

                if (isSelected) { bg = Color.FromArgb("#00B2CA"); txt = Colors.White; } 
                else if (isToday) { bg = Color.FromArgb("#E0F7FA"); txt = Color.FromArgb("#00B2CA"); }

                daysList.Add(new CalendarDay 
                { 
                    DayNumber = d.ToString(), 
                    Date = date,
                    BgColor = bg,
                    TextColor = txt,
                    IsSelected = isSelected
                });
            }

            if (CalendarGrid != null) CalendarGrid.ItemsSource = daysList;
        }

        private void OnDaySelected(object sender, SelectionChangedEventArgs e)
        {
            var day = e.CurrentSelection.FirstOrDefault() as CalendarDay;
            if (day == null || day.Date == null) return;

            _selectedDate = day.Date.Value;
            RenderCalendar(); 
            FiltrarCitasPorFecha(_selectedDate);
        }

        private void FiltrarCitasPorFecha(DateTime fecha)
        {
            if (LblSelectedDate != null)
                LblSelectedDate.Text = "Citas del " + fecha.ToString("dd 'de' MMMM");
            
            var citasDia = _allPacientes
                .Where(p => p.HoraCita.Date == fecha.Date)
                .OrderBy(p => p.HoraCita)
                .ToList();

            if (DailyAppointmentsList != null)
                DailyAppointmentsList.ItemsSource = citasDia;
        }

        private void PrevMonth(object sender, EventArgs e) { _currentMonth = _currentMonth.AddMonths(-1); RenderCalendar(); }
        private void NextMonth(object sender, EventArgs e) { _currentMonth = _currentMonth.AddMonths(1); RenderCalendar(); }
    }
}