namespace NasaValley.Domain;

/// <summary>Progress of the stranded-survivor quest line.</summary>
public enum NpcPhase
{
    None,
    AwaitingSupplies,
    SuppliesDelivered,
    SiteSkipped,
    SiteVisited,
    FinalPending,
    Done,
}

/// <summary>How the mission ends with respect to the survivor.</summary>
public enum NpcOutcome
{
    None,
    Rescued,
    LeftBehind,
    LostContact,
}

/// <summary>Interaction a letter exposes: just read it, choose to go, or the final rescue choice.</summary>
public enum NpcDecision
{
    None,
    Acknowledge,
    GoOrSkip,
    Final,
}

public sealed record NpcLetter(
    string Id,
    string Sender,
    string Subject,
    string Body,
    NpcDecision Decision,
    string SubjectEn = "",
    string BodyEn = "");

/// <summary>
/// Content for the survivor quest. Letters are plain data so the quest can be retuned or
/// localised without touching the simulation. Bodies may contain {x}/{y} placeholders that
/// are filled with the survivor-site coordinates when delivered.
/// </summary>
public static class NpcQuestDatabase
{
    public const string Contact = "npc_contact";
    public const string Coordinates = "npc_coords";
    public const string Skipped = "npc_skipped";
    public const string Found = "npc_found";
    public const string Final = "npc_final";
    public const string FinalLost = "npc_final_lost";

    public static readonly IReadOnlyList<NpcLetter> Letters = new[]
    {
        new NpcLetter(Contact, "Ares-3", "¿Hay alguien ahí?",
            "Ares-7, ¿me escuchan? Soy la doctora Ríos, de la misión Ares-3. " +
            "Mi aterrizador se partió y estoy sola. No te pido que vengas todavía: " +
            "deja 3 de agua en la baliza que marca tu mapa y sabré que no estoy sola.",
            NpcDecision.Acknowledge,
            "Is anyone there?",
            "Ares-7, can you hear me? I'm Dr. Ríos, from the Ares-3 mission. " +
            "My lander broke apart and I'm alone. I'm not asking you to come yet: " +
            "leave 3 water at the beacon marked on your map and I'll know I'm not alone."),
        new NpcLetter(Coordinates, "Ares-3", "Gracias por el agua",
            "No sabes lo que significa. Si te quedan fuerzas, mi refugio está en " +
            "X:{x} · Y:{y}. Está lejos: calcula tu oxígeno antes de venir. " +
            "Si no puedes, lo entiendo.",
            NpcDecision.GoOrSkip,
            "Thanks for the water",
            "You have no idea what this means. If you have the strength, my shelter is at " +
            "X:{x} · Y:{y}. It's far: check your oxygen before you come. " +
            "If you can't, I understand."),
        new NpcLetter(Skipped, "Ares-3", "Te entiendo",
            "No pasa nada. Cuídate y repara tu nave. Si cambias de idea, la baliza seguirá aquí.",
            NpcDecision.Acknowledge,
            "I understand",
            "It's okay. Stay safe and fix your ship. If you change your mind, the beacon will still be here."),
        new NpcLetter(Found, "Ares-3", "No puedo creerlo",
            "¡Has venido! Toma mi pico reforzado, repuestos y un poco de hielo de mi reserva. " +
            "Y cuando arregles tu motor… ¿me llevarías a casa?",
            NpcDecision.Acknowledge,
            "I can't believe it",
            "You came! Take my reinforced pick, spare parts and some ice from my reserve. " +
            "And when you fix your engine… will you take me home?"),
        new NpcLetter(Final, "Ares-3", "Tu motor arrancó",
            "Escuché el despegue. La última decisión es tuya: hay sitio para mí, pero el tanque " +
            "va justo. ¿Me llevas o despegas solo?",
            NpcDecision.Final,
            "Your engine started",
            "I heard the launch. The last choice is yours: there's room for me, but the tank " +
            "is tight. Will you take me or leave alone?"),
        new NpcLetter(FinalLost, "Señal perdida", "Silencio en la baliza",
            "La baliza de Ares-3 dejó de responder. En algún lugar, una superviviente " +
            "se queda mirando el cielo mientras tu nave se aleja.",
            NpcDecision.Acknowledge,
            "Lost signal",
            "The Ares-3 beacon stopped responding. Somewhere, a survivor " +
            "watches the sky as your ship pulls away."),
    };

    public static NpcLetter? Get(string id) => Letters.FirstOrDefault(l => l.Id == id);
}

/// <summary>
/// Drives the survivor quest: interference bursts, letter delivery, supply drop,
/// the optional rescue trip, and the final choice once the ship is repaired.
/// </summary>
public static class NpcQuestSystem
{
    private const string CacheItem = "agua";

    /// <summary>Runs the interference timer and detects world milestones each tick.</summary>
    public static void Update(IGameContext ctx, double dt)
    {
        var s = ctx.State;

        // Interference repeats every N seconds until the player answers, and stops once
        // the quest has moved past the first contact or the ship is ready to launch.
        var repaired = RepairSystem.Count(s) >= ctx.Parts.Parts.Count;
        if (s.NpcPhase == NpcPhase.None && !repaired)
        {
            s.NpcInterferenceTimer -= dt;
            if (s.NpcInterferenceTimer <= 0)
            {
                s.NpcInterferenceTimer = ctx.Balance.NpcInterferenceInterval;
                s.InterferenceActive = true;
                ctx.Say("La radio de la nave capta una señal interferida. Revisa la nave.", MessageKind.Warn);
            }
        }
    }

    public static void ConfigureSites(GameState s, BalanceConfig b)
    {
        s.CacheItemId = CacheItem;
        s.CacheAmount = b.NpcCacheAmount;
        s.NpcInterferenceTimer = b.NpcInterferenceInterval;
    }

    /// <summary>Called when the player opens the Nave while a signal is interfering.</summary>
    public static void Decode(IGameContext ctx)
    {
        var s = ctx.State;
        if (!s.InterferenceActive || s.NpcPhase != NpcPhase.None) return;

        s.InterferenceActive = false;
        s.NpcPhase = NpcPhase.AwaitingSupplies;
        Deliver(ctx, NpcQuestDatabase.Contact);
        ctx.Say("Decodificas el mensaje: alguien más sobrevivió al descenso.", MessageKind.Good);
    }

    /// <summary>The player leaves the requested supplies at the cache.</summary>
    public static bool Deposit(IGameContext ctx)
    {
        var s = ctx.State;
        if (s.NpcPhase != NpcPhase.AwaitingSupplies || s.CacheFilled) return false;

        if (s.Inventory[s.CacheItemId] < s.CacheAmount)
        {
            var missing = s.CacheAmount - s.Inventory[s.CacheItemId];
            ctx.Say(Loc.F("Faltan {0} de {1} para la baliza.", missing, Loc.T(ctx.Items[s.CacheItemId].Name)),
                MessageKind.Warn);
            return false;
        }

        s.Inventory.Remove(s.CacheItemId, s.CacheAmount);
        s.CacheFilled = true;
        s.SiteUnlocked = true;
        s.NpcPhase = NpcPhase.SuppliesDelivered;
        Deliver(ctx, NpcQuestDatabase.Coordinates);
        ctx.Say("Dejas los suministros en la baliza. La señal responde con unas coordenadas.", MessageKind.Good);
        return true;
    }

    /// <summary>The player answers the coordinates letter: go to the site or stay.</summary>
    public static void ChooseSite(IGameContext ctx, bool go)
    {
        var s = ctx.State;
        if (s.NpcPhase != NpcPhase.SuppliesDelivered) return;

        if (go)
        {
            ctx.Say("La baliza marca el camino. Vigila tu oxígeno.", MessageKind.Info);
            return;
        }

        s.SiteSkipped = true;
        s.NpcPhase = NpcPhase.SiteSkipped;
        Deliver(ctx, NpcQuestDatabase.Skipped);
        ctx.Say("Decides no arriesgar tu oxígeno. La baliza queda atrás.", MessageKind.Warn);
    }

    /// <summary>The player reaches the survivor and receives the reward.</summary>
    public static bool MeetSurvivor(IGameContext ctx)
    {
        var s = ctx.State;
        if (!s.SiteUnlocked || s.SurvivorFound || s.SiteSkipped) return false;

        GrantReward(ctx);
        s.SurvivorFound = true;
        s.NpcPhase = NpcPhase.SiteVisited;
        Deliver(ctx, NpcQuestDatabase.Found);
        ctx.Say("Encuentras a la superviviente. Te entrega su equipo.", MessageKind.Good);
        return true;
    }

    /// <summary>Reward that stays inside the loop: tools, materials and a suit/health boost.</summary>
    private static void GrantReward(IGameContext ctx)
    {
        var s = ctx.State;
        if (s.RewardGranted) return;
        s.RewardGranted = true;

        s.Inventory.Add("pico_reforzado", 1);
        s.Inventory.Add("fijaciones", 2);
        s.Inventory.Add("hielo", 3);
        s.Inventory.Add("agua", 2);
        s.Inventory.Add("basalto", 2);

        // One extra ship oxygen charge and a full suit top-up: a small permanent-ish boost.
        s.ShipO2Charges = Math.Min(ctx.Balance.ShipO2ChargesMax, s.ShipO2Charges + 1);
        s.Vitals.O2 = Math.Min(100, s.Vitals.O2 + 25);
        s.Vitals.Energy = Math.Min(100, s.Vitals.Energy + 25);
        s.Vitals.Health = Math.Min(100, s.Vitals.Health + 25);
    }

    /// <summary>
    /// Called when the last ship part is repaired. If the quest progressed, the final
    /// letter is delivered and the launch is held until the player makes a choice.
    /// </summary>
    public static void OnShipRepaired(IGameContext ctx)
    {
        var s = ctx.State;
        if (s.NpcPhase == NpcPhase.FinalPending || s.NpcPhase == NpcPhase.Done) return;

        var progressed = s.NpcPhase is NpcPhase.AwaitingSupplies or NpcPhase.SuppliesDelivered
            or NpcPhase.SiteSkipped or NpcPhase.SiteVisited;
        if (!progressed) return;

        if (s.SurvivorFound)
        {
            s.NpcOutcome = NpcOutcome.None;
            Deliver(ctx, NpcQuestDatabase.Final);
        }
        else
        {
            s.NpcOutcome = NpcOutcome.LostContact;
            Deliver(ctx, NpcQuestDatabase.FinalLost);
        }

        // Do not block the launch: the manual "Despegar" button stays available.
        s.NpcPhase = NpcPhase.FinalPending;
        s.PendingFinalChoice = true;
    }

    /// <summary>Resolves the final choice. For a lost contact this simply acknowledges it.</summary>
    public static void ChooseFinal(IGameContext ctx, bool take)
    {
        var s = ctx.State;
        if (!s.PendingFinalChoice) return;

        if (s.NpcOutcome != NpcOutcome.LostContact)
        {
            s.NpcOutcome = take ? NpcOutcome.Rescued : NpcOutcome.LeftBehind;
        }

        s.PendingFinalChoice = false;
        s.NpcPhase = NpcPhase.Done;

        ctx.Say(s.NpcOutcome switch
        {
            NpcOutcome.Rescued => "La superviviente sube a la nave. No vuelves solo.",
            NpcOutcome.LeftBehind => "Despegas con su señal a la espalda.",
            _ => "La baliza queda en silencio.",
        }, s.NpcOutcome == NpcOutcome.Rescued ? MessageKind.Good : MessageKind.Warn);
    }

    /// <summary>Marks a letter read and focuses it in the Nave.</summary>
    public static void Read(GameState s, string id)
    {
        if (!s.NpcLetters.Contains(id)) return;
        s.ActiveNpcLetterId = id;
        s.NpcUnread.Remove(id);
    }

    private static void Deliver(IGameContext ctx, string id)
    {
        var s = ctx.State;
        if (!s.NpcLetters.Contains(id)) s.NpcLetters.Add(id);
        if (!s.NpcUnread.Contains(id)) s.NpcUnread.Add(id);
        s.ActiveNpcLetterId = id;
        ctx.Events.RaiseLetterDelivered(id);
    }

    /// <summary>Fills {x}/{y} placeholders with the survivor-site coordinates.</summary>
    public static string BodyFor(NpcLetter letter, GameState s)
    {
        var body = Loc.IsEnglish && letter.BodyEn.Length > 0 ? letter.BodyEn : letter.Body;
        return body
            .Replace("{x}", ((int)s.SurvivorX).ToString())
            .Replace("{y}", ((int)s.SurvivorY).ToString());
    }

    public static string SubjectFor(NpcLetter letter) =>
        Loc.IsEnglish && letter.SubjectEn.Length > 0 ? letter.SubjectEn : letter.Subject;

    public static int UnreadCount(GameState s) => s.NpcUnread.Count;
}
