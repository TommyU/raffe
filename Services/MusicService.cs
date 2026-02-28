using System;
using System.IO;
using System.Windows.Media;

namespace Raffe.Services;

/// <summary>
/// Plays looping background music via WPF MediaPlayer.
/// All calls must be made on the UI (STA) thread.
/// </summary>
public sealed class MusicService : IDisposable
{
    private readonly MediaPlayer _player = new();
    private string? _currentPath;
    private bool _loop = true;
    private double _volume = 0.7;

    public double Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0, 1);
            _player.Volume = _volume;
        }
    }

    public MusicService()
    {
        _player.MediaEnded += OnMediaEnded;
    }

    /// <summary>
    /// Play the given file. Silently ignored when path is empty or file not found.
    /// Won't restart if the same file is already playing.
    /// </summary>
    /// <param name="path">Audio file path.</param>
    /// <param name="loop">True (default) to loop; false to play once.</param>
    public void Play(string? path, bool loop = true)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Stop();
            return;
        }

        if (string.Equals(_currentPath, path, StringComparison.OrdinalIgnoreCase) &&
            _player.Source != null)
            return;

        _loop = loop;
        _currentPath = path;
        _player.Open(new Uri(path, UriKind.Absolute));
        _player.Volume = _volume;
        _player.Play();
    }

    public void Stop()
    {
        _currentPath = null;
        _player.Stop();
        _player.Close();
    }

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        if (!_loop || _currentPath == null) return;
        _player.Position = TimeSpan.Zero;
        _player.Play();
    }

    public void Dispose()
    {
        _player.MediaEnded -= OnMediaEnded;
        _player.Stop();
        _player.Close();
    }
}
