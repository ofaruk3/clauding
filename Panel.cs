namespace Clauding;

/// <summary>
/// The shortcuts panel: every action with its keyboard shortcut (guide), the same list to record new
/// shortcuts (editor), and the macro pad drawing when a pad is on. Topmost, centred, closes with Esc.
/// Also the "hold the modifiers" cheat sheet (same page, guide mode, closes when the keys are released).
/// </summary>
sealed class Panel : WebPencere
{
    bool rehberIcin;

    public Panel() : base("panel.html", false)
    {
        TopMost = true;
        KeyPreview = true;
    }

    public void Ac(string durumJson, bool rehber)
    {
        rehberIcin = rehber;
        if (!Visible) Ortala(1240, 860);
        Gonder(durumJson);
        Show(); // shown without activation; the cheat sheet must not steal the keys being held
        if (!rehber) { Activate(); web.Focus(); }
    }

    /// <summary>The cheat sheet goes away when the modifier keys are released (the editor never does).</summary>
    public void RehberKapat() { if (rehberIcin && Visible) Kapat(); }

    public void Kapat()
    {
        rehberIcin = false;
        Gonder("{\"t\":\"kapaniyor\"}");
        var z = new System.Windows.Forms.Timer { Interval = 180 };
        z.Tick += (_, _) => { z.Stop(); z.Dispose(); Hide(); };
        z.Start();
    }

    protected override bool ShowWithoutActivation => true;

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        Gonder("{\"t\":\"odakKayip\"}"); // the guide closes itself when you click elsewhere; the editor stays
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Kapat(); return; }
        base.OnFormClosing(e);
    }
}

/// <summary>Onboarding (first run, card by card) and Settings (same cards behind a sidebar).</summary>
sealed class KurulumPenceresi : WebPencere
{
    public readonly bool Onboarding;
    public Action? DurumIste;

    public KurulumPenceresi(bool onboarding) : base("kurulum/index.html?mod=" + (onboarding ? "onboarding" : "ayarlar"), true)
    {
        Onboarding = onboarding;
        Hazir += () => DurumIste?.Invoke();
    }

    public void DurumGonder() => DurumIste?.Invoke();
}
