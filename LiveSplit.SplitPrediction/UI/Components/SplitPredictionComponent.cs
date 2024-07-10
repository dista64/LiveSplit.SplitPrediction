using LiveSplit.Model;
using LiveSplit.Model.Comparisons;
using LiveSplit.SplitPrediction;
using LiveSplit.TimeFormatters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.AxHost;

namespace LiveSplit.UI.Components
{
    public class SplitPredictionComponent : IComponent
    {
        protected InfoTimeComponent InternalComponent { get; set; }
        protected SplitPredictionSettings Settings { get; set; }
        private SplitTimeFormatter Formatter { get; set; }
        private string PreviousInformationName { get; set; }

        public float PaddingTop => InternalComponent.PaddingTop;
        public float PaddingLeft => InternalComponent.PaddingLeft;
        public float PaddingBottom => InternalComponent.PaddingBottom;
        public float PaddingRight => InternalComponent.PaddingRight;

        public IDictionary<string, Action> ContextMenuControls => null;


        public SplitPredictionComponent(LiveSplitState state)
        {
            Settings = new SplitPredictionSettings() { 
                CurrentState = state
            };
            Formatter = new SplitTimeFormatter(Settings.Accuracy);
            InternalComponent = new InfoTimeComponent(null, null, Formatter);
            state.ComparisonRenamed += state_ComparisonRenamed;
        }

        void state_ComparisonRenamed(object sender, EventArgs e)
        {
            var args = (RenameEventArgs)e;
            if (Settings.Comparison == args.OldName)
            {
                Settings.Comparison = args.NewName;
                ((LiveSplitState)sender).Layout.HasChanged = true;
            }
        }

        private void PrepareDraw(LiveSplitState state)
        {
            InternalComponent.DisplayTwoRows = Settings.Display2Rows;

            InternalComponent.NameLabel.HasShadow
                = InternalComponent.ValueLabel.HasShadow
                = state.LayoutSettings.DropShadows;

            Formatter.Accuracy = Settings.Accuracy;

            InternalComponent.NameLabel.ForeColor = Settings.OverrideTextColor ? Settings.TextColor : state.LayoutSettings.TextColor;
            InternalComponent.ValueLabel.ForeColor = Settings.OverrideTimeColor ? Settings.TimeColor : state.LayoutSettings.TextColor;
        }

        private void DrawBackground(Graphics g, LiveSplitState state, float width, float height)
        {
            if(Settings.BackgroundColor.A > 0
                || Settings.BackgroundGradient != GradientType.Plain
                && Settings.BackgroundColor2.A > 0)
            {
                var gradientBrush = new LinearGradientBrush(
                    new PointF(0, 0),
                            Settings.BackgroundGradient == GradientType.Horizontal
                            ? new PointF(width, 0)
                            : new PointF(0, height),
                            Settings.BackgroundColor,
                            Settings.BackgroundGradient == GradientType.Plain
                            ? Settings.BackgroundColor
                            : Settings.BackgroundColor2);
                g.FillRectangle(gradientBrush, 0, 0, width, height);
            }
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            DrawBackground(g, state, HorizontalWidth, height);
            PrepareDraw(state);
            InternalComponent.DrawHorizontal(g, state, height, clipRegion);
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            DrawBackground(g, state, width, VerticalHeight);
            PrepareDraw(state);
            InternalComponent.DrawVertical(g, state, width, clipRegion);
        }

        public float VerticalHeight => InternalComponent.VerticalHeight;

        public float MinimumWidth => InternalComponent.MinimumWidth;

        public float HorizontalWidth => InternalComponent.HorizontalWidth;

        public float MinimumHeight => InternalComponent.MinimumHeight;

        public string ComponentName => GetDisplayedName(Settings.Comparison);

        public Control GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public void SetSettings(XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        protected string GetDisplayedName(string comparison)
        {
            switch (comparison)
            {
                case "Current Comparison":
                    return "Current " + Settings.Split + "  Pace"; // Current splitName Pace
                case Run.PersonalBestComparisonName:
                    return "Current " + Settings.Split + "  Pace"; // Current splitName Pace
                case BestSegmentsComparisonGenerator.ComparisonName:
                    return "Best Possible " + Settings.Split; // Best Possible splitName
                case WorstSegmentsComparisonGenerator.ComparisonName:
                    return "Worst Possible " + Settings.Split; // Worst Possible splitName
                case AverageSegmentsComparisonGenerator.ComparisonName:
                    return Settings.Split + " Prediction"; // Predicted splitName, splitName Prediction (unsure I think I like the 2nd one better)
                default:
                    return "Current " + Settings.Split + " Pace (" + CompositeComparisons.GetShortComparisonName(comparison) + ")"; // Current splitName Pace (comparisonNAme)
            }
        }

        // Calculations done here I think for the prediction. For now this is basically copy/paste from run prediction component.
        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            var comparison = Settings.Comparison == "Current Comparison" ? state.CurrentComparison : Settings.Comparison;
            if (!state.Run.Comparisons.Contains(comparison))
                comparison = state.CurrentComparison;

            InternalComponent.InformationName = InternalComponent.LongestString = GetDisplayedName(comparison);

            if (InternalComponent.InformationName.StartsWith("Current Pace") && state.CurrentPhase == TimerPhase.NotRunning)
            {
                InternalComponent.TimeValue = null;
            }
            else if (state.CurrentPhase == TimerPhase.Running || state.CurrentPhase == TimerPhase.Paused)
            {
                TimeSpan? delta = LiveSplitStateHelper.GetLastDelta(state, state.CurrentSplitIndex, comparison, state.CurrentTimingMethod) ?? TimeSpan.Zero;
                var liveDelta = state.CurrentTime[state.CurrentTimingMethod] - state.CurrentSplit.Comparisons[comparison][state.CurrentTimingMethod];
                if (liveDelta > delta)
                    delta = liveDelta;
                InternalComponent.TimeValue = delta + state.Run.Last().Comparisons[comparison][state.CurrentTimingMethod];
                
            }
            else if (state.CurrentPhase == TimerPhase.Ended)
            {
                InternalComponent.TimeValue = state.Run.Last().SplitTime[state.CurrentTimingMethod];
            }
            else
            {
                InternalComponent.TimeValue = state.Run.Last().Comparisons[comparison][state.CurrentTimingMethod];
            }

            InternalComponent.Update(invalidator, state, width, height, mode);
        }

        

        

        public void Dispose()
        {
        }

        public int GetSettingsHashCode() => Settings.GetSettingsHashCode();
    }
}