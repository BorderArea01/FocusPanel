using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Threading;
using FocusPanel.Services;
using FocusPanel.Data;
using FocusPanel.Models;
using System.Linq;

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
        LoadStats();
    }

    private void LoadStats()
    {
        try
        {
            using (var context = new AppDbContext())
            {
                // Ensure schema exists just in case viewmodel loads before app (unlikely but safe)
                context.EnsureSchema();
                
                CompletedPomodoros = context.PomodoroSessions.Count(s => s.Status == "Completed");
                TotalFocusMinutes = context.PomodoroSessions
                    .Where(s => s.Status == "Completed")
                    .ToList() // Client evaluation for Sum might be needed if SQLite doesn't support Sum on int? No, Sum works. But safer to materialize if empty.
                    .Sum(s => s.DurationMinutes);
            }
        }
        catch { }
    }

    private void SaveSession(int durationMinutes, string status)
    {
        try
        {
            using (var context = new AppDbContext())
            {
                context.PomodoroSessions.Add(new PomodoroSession
                {
                    StartTime = DateTime.Now.AddMinutes(-durationMinutes),
                    EndTime = DateTime.Now,
                    DurationMinutes = durationMinutes,
                    Status = status
                });
                context.SaveChanges();
            }
        }
        catch { }
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
            
            int duration = (int)_totalTime.TotalMinutes;
            SaveSession(duration, "Completed");
            
            CompletedPomodoros++;
            TotalFocusMinutes += duration;
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
