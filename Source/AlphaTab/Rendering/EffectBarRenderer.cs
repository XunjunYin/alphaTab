﻿using System.Collections.Generic;
using System.Windows.Controls;
using AlphaTab.Collections;
using AlphaTab.Model;
using AlphaTab.Platform;
using AlphaTab.Rendering.Glyphs;
using AlphaTab.Rendering.Layout;

namespace AlphaTab.Rendering
{
    /// <summary>
    /// This renderer is responsible for displaying effects above or below the other staves
    /// like the vibrato. 
    /// </summary>
    public class EffectBarRenderer : GroupedBarRenderer
    {
        private IEffectBarRendererInfo _info;
        private FastList<FastList<EffectGlyph>> _uniqueEffectGlyphs;
        private FastList<FastDictionary<int, EffectGlyph>> _effectGlyphs;
        private Beat _lastBeat;

        public EffectBarRenderer(Bar bar, IEffectBarRendererInfo info)
            : base(bar)
        {
            _info = info;
            _uniqueEffectGlyphs = new FastList<FastList<EffectGlyph>>();
            _effectGlyphs = new FastList<FastDictionary<int, EffectGlyph>>();
        }

        public override void DoLayout()
        {
            base.DoLayout();
            if (Index == 0)
            {
                Stave.TopSpacing = 5;
                Stave.BottomSpacing = 5;
            }
            Height = _info.GetHeight(this);
        }

        public override void FinalizeRenderer(ScoreLayout layout)
        {
            base.FinalizeRenderer(layout);
            // after all layouting and sizing place and size the effect glyphs
            IsEmpty = true;

#if MULTIVOICE_SUPPORT
        foreach (var v in Bar.Voices)
        {
            Glyph prevGlyph = null;
            if (Index > 0)
            {
                // check if previous renderer had an effect on his last beat
                // and use this as merging element
                EffectBarRenderer prevRenderer = (EffectBarRenderer) Stave.BarRenderers[Index - 1];
                if (prevRenderer._lastBeat != null)
                {
                    prevGlyph = prevRenderer._effectGlyphs[v.Index][prevRenderer._lastBeat.Index];
                }
            }
            foreach (var beatIndex in _effectGlyphs[v.Index].Keys)
            {
                Glyph effect = _effectGlyphs[v.Index][beatIndex];
                
                AlignGlyph(_info.SizingMode, beatIndex, 0, prevGlyph);
                
                prevGlyph = effect;
                IsEmpty = false;
            }
            
        }
#else
            EffectGlyph prevGlyph = null;
            if (Index > 0)
            {
                // check if previous renderer had an effect on his last beat
                // and use this as merging element
                EffectBarRenderer prevRenderer = (EffectBarRenderer)Stave.BarRenderers[Index - 1];
                if (prevRenderer._lastBeat != null && prevRenderer._effectGlyphs[0].ContainsKey(prevRenderer._lastBeat.Index))
                {
                    prevGlyph = prevRenderer._effectGlyphs[0][prevRenderer._lastBeat.Index];
                }
            }
            foreach (var key in _effectGlyphs[0].Keys)
            {
                int beatIndex = Std.ParseInt(key);
                EffectGlyph effect = _effectGlyphs[0][beatIndex];

                AlignGlyph(_info.SizingMode, beatIndex, 0, prevGlyph);

                prevGlyph = effect;
                IsEmpty = false;
            }
#endif
        }

        private void AlignGlyph(EffectBarGlyphSizing sizing, int beatIndex, int voiceIndex, EffectGlyph prevGlyph)
        {
            EffectGlyph g = _effectGlyphs[voiceIndex][beatIndex];
            Glyph pos;
            var container = GetBeatContainer(voiceIndex, beatIndex);
            switch (sizing)
            {
                case EffectBarGlyphSizing.SinglePreBeatOnly:
                    pos = container.PreNotes;
                    g.X = pos.X + container.X;
                    g.Width = pos.Width;
                    break;

                case EffectBarGlyphSizing.SinglePreBeatToOnBeat:
                    pos = container.PreNotes;
                    g.X = pos.X + container.X;
                    pos = container.OnNotes;
                    g.Width = (pos.X + container.X + pos.Width) - g.X;
                    break;

                case EffectBarGlyphSizing.SinglePreBeatToPostBeat:
                    pos = container.PreNotes;
                    g.X = pos.X + container.X;
                    pos = container.PostNotes;
                    g.Width = (pos.X + container.X + pos.Width) - g.X;
                    break;

                case EffectBarGlyphSizing.SingleOnBeatOnly:
                    pos = container.OnNotes;
                    g.X = pos.X + container.X;
                    g.Width = pos.Width;
                    break;

                case EffectBarGlyphSizing.SingleOnBeatToPostBeat:
                    pos = container.OnNotes;
                    g.X = pos.X + container.X;
                    pos = container.PostNotes;
                    g.Width = (pos.X + container.X + pos.Width) - g.X;
                    break;

                case EffectBarGlyphSizing.SinglePostBeatOnly:
                    pos = container.PostNotes;
                    g.X = pos.X + container.X;
                    g.Width = pos.Width;
                    break;

                case EffectBarGlyphSizing.GroupedPreBeatOnly:
                    if (g != prevGlyph) { AlignGlyph(EffectBarGlyphSizing.SinglePreBeatOnly, beatIndex, voiceIndex, prevGlyph); }
                    else
                    {
                        pos = container.PreNotes;
                        var posR = (EffectBarRenderer)pos.Renderer;
                        var gR = (EffectBarRenderer)g.Renderer;
                        g.Width = (posR.X + posR.BeatGlyphsStart + container.X + pos.X + pos.Width) - (gR.X + gR.BeatGlyphsStart + g.X);
                        g.ExpandTo(container.Beat);
                    }
                    break;

                case EffectBarGlyphSizing.GroupedPreBeatToOnBeat:
                    if (g != prevGlyph) { AlignGlyph(EffectBarGlyphSizing.SinglePreBeatToOnBeat, beatIndex, voiceIndex, prevGlyph); }
                    else
                    {
                        pos = container.OnNotes;
                        var posR = (EffectBarRenderer)pos.Renderer;
                        var gR = (EffectBarRenderer)g.Renderer;
                        g.Width = (posR.X + posR.BeatGlyphsStart + container.X + pos.X + pos.Width) - (gR.X + gR.BeatGlyphsStart + g.X);
                        g.ExpandTo(container.Beat);
                    }
                    break;

                case EffectBarGlyphSizing.GroupedPreBeatToPostBeat:
                    if (g != prevGlyph) { AlignGlyph(EffectBarGlyphSizing.SinglePreBeatToPostBeat, beatIndex, voiceIndex, prevGlyph); }
                    else
                    {
                        pos = container.PostNotes;
                        var posR = (EffectBarRenderer)pos.Renderer;
                        var gR = (EffectBarRenderer)g.Renderer;
                        g.Width = (posR.X + posR.BeatGlyphsStart + container.X + pos.X + pos.Width) - (gR.X + gR.BeatGlyphsStart + g.X);
                        g.ExpandTo(container.Beat);
                    }
                    break;

                case EffectBarGlyphSizing.GroupedOnBeatOnly:
                    if (g != prevGlyph) { AlignGlyph(EffectBarGlyphSizing.SingleOnBeatOnly, beatIndex, voiceIndex, prevGlyph); }
                    else
                    {
                        pos = container.OnNotes;
                        var posR = (EffectBarRenderer)pos.Renderer;
                        var gR = (EffectBarRenderer)g.Renderer;
                        g.Width = (posR.X + posR.BeatGlyphsStart + container.X + pos.X + pos.Width) - (gR.X + gR.BeatGlyphsStart + g.X);
                        g.ExpandTo(container.Beat);
                    }
                    break;

                case EffectBarGlyphSizing.GroupedOnBeatToPostBeat:
                    if (g != prevGlyph) { AlignGlyph(EffectBarGlyphSizing.SingleOnBeatToPostBeat, beatIndex, voiceIndex, prevGlyph); }
                    else
                    {
                        pos = container.PostNotes;
                        var posR = (EffectBarRenderer)pos.Renderer;
                        var gR = (EffectBarRenderer)g.Renderer;
                        g.Width = (posR.X + posR.BeatGlyphsStart + container.X + pos.X + pos.Width) - (gR.X + gR.BeatGlyphsStart + g.X);
                        g.ExpandTo(container.Beat);
                    }
                    break;

                case EffectBarGlyphSizing.GroupedPostBeatOnly:
                    if (g != prevGlyph) { AlignGlyph(EffectBarGlyphSizing.GroupedPostBeatOnly, beatIndex, voiceIndex, prevGlyph); }
                    else
                    {
                        pos = container.PostNotes;
                        var posR = (EffectBarRenderer)pos.Renderer;
                        var gR = (EffectBarRenderer)g.Renderer;
                        g.Width = (posR.X + posR.BeatGlyphsStart + container.X + pos.X + pos.Width) - (gR.X + gR.BeatGlyphsStart + g.X);
                        g.ExpandTo(container.Beat);
                    }
                    break;
            }
        }

        protected override void CreatePreBeatGlyphs()
        {
        }

        protected override void CreateBeatGlyphs()
        {
#if MULTIVOICE_SUPPORT
            foreach (var v in Bar.Voices)
            {
                _effectGlyphs.Add(new Dictionary<int, EffectGlyph>());
                _uniqueEffectGlyphs.Add(new FastList<EffectGlyph>());
                CreateVoiceGlyphs(v);
            }
#else
            _effectGlyphs.Add(new FastDictionary<int, EffectGlyph>());
            _uniqueEffectGlyphs.Add(new FastList<EffectGlyph>());
            CreateVoiceGlyphs(Bar.Voices[0]);
#endif
        }

        private void CreateVoiceGlyphs(Voice v)
        {
            for (int i = 0; i < v.Beats.Count; i++)
            {
                var b = v.Beats[i];
                // we create empty glyphs as alignment references and to get the 
                // effect bar sized
                var container = new BeatContainerGlyph(b);
                container.PreNotes = new BeatGlyphBase();
                container.OnNotes = new BeatGlyphBase();
                container.PostNotes = new BeatGlyphBase();
                AddBeatGlyph(container);

                if (_info.ShouldCreateGlyph(this, b))
                {
                    CreateOrResizeGlyph(_info.SizingMode, b);
                }

                _lastBeat = b;
            }
        }

        private void CreateOrResizeGlyph(EffectBarGlyphSizing sizing, Beat b)
        {
            switch (sizing)
            {
                case EffectBarGlyphSizing.SinglePreBeatOnly:
                case EffectBarGlyphSizing.SinglePreBeatToOnBeat:
                case EffectBarGlyphSizing.SinglePreBeatToPostBeat:
                case EffectBarGlyphSizing.SingleOnBeatOnly:
                case EffectBarGlyphSizing.SingleOnBeatToPostBeat:
                case EffectBarGlyphSizing.SinglePostBeatOnly:
                    var g = _info.CreateNewGlyph(this, b);
                    g.Renderer = this;
                    g.DoLayout();
                    _effectGlyphs[b.Voice.Index][b.Index] = g;
                    _uniqueEffectGlyphs[b.Voice.Index].Add(g);
                    break;

                case EffectBarGlyphSizing.GroupedPreBeatOnly:
                case EffectBarGlyphSizing.GroupedPreBeatToOnBeat:
                case EffectBarGlyphSizing.GroupedPreBeatToPostBeat:
                case EffectBarGlyphSizing.GroupedOnBeatOnly:
                case EffectBarGlyphSizing.GroupedOnBeatToPostBeat:
                case EffectBarGlyphSizing.GroupedPostBeatOnly:
                    if (b.Index > 0 || Index > 0)
                    {
                        // check if the previous beat also had this effect
                        Beat prevBeat = b.PreviousBeat;
                        if (_info.ShouldCreateGlyph(this, prevBeat))
                        {
                            // expand the previous effect
                            EffectGlyph prevEffect = null;
                            if (b.Index > 0 && _effectGlyphs[b.Voice.Index].ContainsKey(prevBeat.Index))
                            {
                                prevEffect = _effectGlyphs[b.Voice.Index][prevBeat.Index];
                            }
                            else if(Index > 0)
                            {
                                var previousRenderer = ((EffectBarRenderer) (Stave.BarRenderers[Index - 1]));
                                var voiceGlyphs = previousRenderer._effectGlyphs[b.Voice.Index];
                                if (voiceGlyphs.ContainsKey(prevBeat.Index))
                                {
                                    prevEffect = voiceGlyphs[prevBeat.Index];
                                }
                            }

                            if (prevEffect == null || !_info.CanExpand(this, prevBeat, b))
                            {
                                CreateOrResizeGlyph(EffectBarGlyphSizing.SinglePreBeatOnly, b);
                            }
                            else
                            {
                                _effectGlyphs[b.Voice.Index][b.Index] = prevEffect;
                            }
                        }
                        else
                        {
                            CreateOrResizeGlyph(EffectBarGlyphSizing.SinglePreBeatOnly, b);
                        }
                    }
                    else
                    {
                        CreateOrResizeGlyph(EffectBarGlyphSizing.SinglePreBeatOnly, b);
                    }
                    break;
            }
        }

        protected override void CreatePostBeatGlyphs()
        {

        }

        public override int TopPadding
        {
            get { return 0; }
        }

        public override int BottomPadding
        {
            get { return 0; }
        }

        protected override void PaintBackground(int cx, int cy, ICanvas canvas)
        {

        }

        public override void Paint(int cx, int cy, ICanvas canvas)
        {
            base.Paint(cx, cy, canvas);

            // canvas.setColor(new Color(0, 0, 200, 100));
            // canvas.fillRect(cx + x, cy + y, width, height);

            var glyphStart = BeatGlyphsStart;

            for (int i = 0; i < _uniqueEffectGlyphs.Count; i++)
            {
                var v = _uniqueEffectGlyphs[i];
                for (int j = 0; j < v.Count; j++)
                {
                    var g = v[j];
                    if (g.Renderer == this)
                    {
                        g.Paint(cx + X + glyphStart, cy + Y, canvas);
                    }
                }
            }
        }
    }
}