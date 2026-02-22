using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Threading;
using FocusPanel.Services;

namespace FocusPanel.ViewModels;

public partial class PomodoroViewModel : ObservableObject
{
    private DispatcherTimer _timer;
    private TimeSpan _timeRemaining;
    private TimeSpan _totalTime;

    [ObservableProperty]
    private string timerDisplay = "25:00";

    [ObservableProperty]
    private string statusMessage = "Ready to Focus";

    [ObservableProperty]
    private double progress = 100;

    [ObservableProperty]
    private int completedPomodoros = 0;

    [ObservableProperty]
    private double totalFocusMinutes = 0;

    [ObservableProperty]
    private bool isRunning;

    public PomodoroViewModel()
    {
        _totalTime = TimeSpan.FromMinutes(25);
        _timeRemaining = _totalTime;
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;
        UpdateDisplay();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_timeRemaining.TotalSeconds > 0)
        {
            _timeRemaining = _timeRemaining.Subtract(TimeSpan.FromSeconds(1));
            UpdateDisplay();
        }
        else
        {
            StopTimer();
            StatusMessage = "Time's up!";
            CompletedPomodoros++;
            TotalFocusMinutes += _totalTime.TotalMinutes;
            // TODO: Play sound or notify
        }
    }

    private void UpdateDisplay()
    {
        TimerDisplay = _timeRemaining.ToString(@"mm\:ss");
        // Update progress logic if needed
    }

    [RelayCommand]
    private void Start()
    {
        if (!IsRunning)
        {
            _timer.Start();
            IsRunning = true;
            StatusMessage = "Focusing...";
            // Open floating windows here
            OpenOverlayWindows();
        }
    }

    [RelayCommand]
    private void Pause()
    {
        if (IsRunning)
        {
            _timer.Stop();
            IsRunning = false;
            StatusMessage = "Paused";
        }
    }

    [RelayCommand]
    private void Reset()
    {
        _timer.Stop();
        IsRunning = false;
        _timeRemaining = _totalTime;
        UpdateDisplay();
        StatusMessage = "Ready to Focus";
        CloseOverlayWindows();
    }

    private void StopTimer()
    {
        _timer.Stop();
        IsRunning = false;
        CloseOverlayWindows();
    }

    private void OpenOverlayWindows()
    {
        PomodoroWindowManager.OpenWindows(this);
    }

    private void CloseOverlayWindows()
    {
        PomodoroWindowManager.CloseWindows();
    }
}
