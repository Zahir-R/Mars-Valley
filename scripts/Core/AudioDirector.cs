using Godot;
using NasaValley.Domain;

namespace NasaValley.Core;

/// <summary>
/// Owns the ambient loops (music, storm, footsteps, low-O2 alarm) and translates
/// world state into sound events: mining hits, pickups, structure placement,
/// crafting and the win/lose stingers. Listens to the domain events.
/// </summary>
public partial class AudioDirector : Node
{
    private const string Sounds = "res://assets/sounds/sounds/";

    public GameSession? Session { get; set; }

    private AudioStreamPlayer _music = null!;
    private AudioStreamPlayer _storm = null!;
    private AudioStreamPlayer _steps = null!;
    private AudioStreamPlayer _pico = null!;
    private double _o2Timer;
    private double _picoCutTimer;
    private int _shakeNodes;

    public override void _Ready()
    {
        AudioSfx.Register("recoger", Sounds + "pick up1.mp3", this);
        AudioSfx.Register("martillo", Sounds + "martillogolpeando.mp3", this);
        AudioSfx.Register("colocar", Sounds + "colocarstructuras.mp3", this);
        AudioSfx.Register("triturador", Sounds + "triturador.mp3", this);
        AudioSfx.Register("inventario", Sounds + "Abrir_cerrar_Inventario.mp3", this);
        AudioSfx.Register("descanso", Sounds + "ventilacion_presurizacion.mp3", this);
        AudioSfx.Register("o2", Sounds + "AlertaOxigeno.mp3", this);
        AudioSfx.Register("mensaje", Sounds + "radiostation.mp3", this);
        AudioSfx.Register("victoria", Sounds + "Descubrimientonuevo.mp3", this);
        AudioSfx.Register("derrota", Sounds + "GameOver.mp3", this);

        _music = CreateLooped(Sounds + "MusciaAmbiente1.mp3", -13f);
        _storm = CreateLooped(Sounds + "tormentaPolvoSuave.mp3", -20f);
        _steps = CreateLooped(Sounds + "sandsteps.mp3", -18f);

        _pico = new AudioStreamPlayer
        {
            Stream = LoadStream(Sounds + "picogolpeando.mp3", loop: false),
            VolumeDb = -8f,
            Bus = "Master",
        };
        AddChild(_pico);

        _music.Play();

        if (Session == null) return;
        Session.Events.Message += OnMessage;
        Session.Events.GameEnded += OnGameEnded;
        Session.Events.LetterDelivered += OnLetterDelivered;
    }

    private static void OnLetterDelivered(string letterId) => AudioSfx.Play("mensaje", -6f);

    private AudioStreamPlayer CreateLooped(string path, float volumeDb)
    {
        var player = new AudioStreamPlayer
        {
            Stream = LoadStream(path, loop: true),
            VolumeDb = volumeDb,
            Bus = "Master",
        };
        AddChild(player);
        return player;
    }

    private static AudioStream LoadStream(string path, bool loop)
    {
        if (GD.Load<AudioStream>(path) is AudioStreamMP3 mp3)
        {
            mp3.Loop = loop;
            return mp3;
        }
        return GD.Load<AudioStream>(path);
    }

    public override void _Process(double delta)
    {
        var s = Session?.State;
        if (s == null) return;

        var storming = s.Storm.Left > 0;
        if (storming != _storm.Playing)
        {
            if (storming) _storm.Play(); else _storm.Stop();
        }

        var walking = s.Moving && s.Running && !s.Paused && !s.ModalOpen;
        if (walking != _steps.Playing)
        {
            if (walking) _steps.Play(); else _steps.Stop();
        }

        _o2Timer += delta;
        if (_o2Timer >= 20 && s.Vitals.O2 < 22.0)
        {
            _o2Timer = 0;
            AudioSfx.Play("o2", -6f);
        }

        if (_picoCutTimer > 0)
        {
            _picoCutTimer -= delta;
            if (_picoCutTimer <= 0 && _pico.Playing) _pico.Stop();
        }

        var shaking = 0;
        foreach (var node in s.Nodes) shaking += node.Shake > 0 ? 1 : 0;
        if (shaking > _shakeNodes)
        {
            _pico.Play();
            _picoCutTimer = 0.4;
        }
        _shakeNodes = shaking;
    }

    private static void OnMessage(string text, MessageKind kind)
    {
        if (text.Contains("Recoges")) AudioSfx.Play("recoger", -10f);
        else if (text.Contains("Fabricado")) AudioSfx.Play("triturador", -8f);
        else if (text.Contains("Colocas") || text.Contains("construyes")) AudioSfx.Play("colocar", -4f);
        else if (text.Contains("Reparado")) AudioSfx.Play("martillo", -8f);
        else if (text.Contains("Descansas")) AudioSfx.Play("descanso", -8f);
    }

    private static void OnGameEnded(bool win) =>
        AudioSfx.Play(win ? "victoria" : "derrota", -3f);
}
