namespace MauiApp1;

public partial class StatsPage : ContentPage
{
    public StatsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var records =
            await DatabaseService
                .GetRecordsAsync();

        TotalLabel.Text =
            records.Count.ToString();

        ManualLabel.Text =
            records.Count(x => x.IsManual)
                .ToString();

        ScannedLabel.Text =
            records.Count(
                x => x.LastScannedAt.HasValue)
                .ToString();

        TodayLabel.Text =
            records.Count(x =>
                x.ActivityDate.HasValue &&
                x.ActivityDate.Value.Date ==
                DateTime.Today
            ).ToString();

        DateTime week =
            DateTime.Now.AddDays(-7);

        WeekLabel.Text =
            records.Count(x =>
                x.ActivityDate.HasValue &&
                x.ActivityDate.Value >= week
            ).ToString();
    }
}