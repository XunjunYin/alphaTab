﻿/*
 * This file is part of alphaTab.
 * Copyright (c) 2014, Daniel Kuschny and Contributors, All rights reserved.
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3.0 of the License, or at your option any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library.
 */

using System;
using AlphaTab.Collections;
using AlphaTab.Model;
using AlphaTab.Platform;

namespace AlphaTab.Rendering.Staves
{
    /// <summary>
    /// This public class stores size information about a stave. 
    /// It is used by the layout engine to collect the sizes of score parts
    /// to align the parts across multiple staves.
    /// </summary>
    public class BarLayoutingInfo
    {
        private const int MinDuration = 30;
        private const int MinDurationWidth = 10;

        private FastList<Spring> _timeSortedSprings;
        private float _xMin;

        private float _onTimePositionsForce;
        private FastDictionary<int, float> _onTimePositions;


        public FastDictionary<int, float> PreBeatSizes { get; set; }
        public FastDictionary<int, float> OnBeatSizes { get; set; }

        public float PreBeatSize { get; set; }
        public float PostBeatSize { get; set; }

        public float VoiceSize { get; set; }
        public float MinStretchForce { get; set; }
        public float TotalSpringConstant { get; set; }

        public BarLayoutingInfo()
        {
            PreBeatSizes = new FastDictionary<int, float>();
            OnBeatSizes = new FastDictionary<int, float>();
            VoiceSize = 0;
            Springs = new FastDictionary<int, Spring>();
        }

        public void UpdateVoiceSize(float size)
        {
            if (size > VoiceSize)
            {
                VoiceSize = size;
            }
        }

        public void SetPreBeatSize(Beat beat, float size)
        {
            if (!PreBeatSizes.ContainsKey(beat.Index) || PreBeatSizes[beat.Index] < size)
            {
                PreBeatSizes[beat.Index] = size;
            }
        }

        public float GetPreBeatSize(Beat beat)
        {
            if (PreBeatSizes.ContainsKey(beat.Index))
            {
                return PreBeatSizes[beat.Index];
            }
            return 0;
        }

        public void SetOnBeatSize(Beat beat, float size)
        {
            if (!OnBeatSizes.ContainsKey(beat.Index) || OnBeatSizes[beat.Index] < size)
            {
                OnBeatSizes[beat.Index] = size;
            }
        }

        public float GetOnBeatSize(Beat beat)
        {
            if (OnBeatSizes.ContainsKey(beat.Index))
            {
                return OnBeatSizes[beat.Index];
            }
            return 0;
        }

        public void UpdateMinStretchForce(float force)
        {
            if (MinStretchForce < force)
            {
                MinStretchForce = force;
            }
        }

        public FastDictionary<int, Spring> Springs { get; set; }
        public int SmallestDuration { get; set; }

        public Spring AddSpring(int start, int duration, float springSize, float preSpringSize, float postSpringSize)
        {
            Spring spring;
            if (!Springs.ContainsKey(start))
            {
                spring = new Spring();
                spring.TimePosition = start;
                spring.SmallestDuration = duration;
                spring.LongestDuration = duration;
                spring.SpringWidth = springSize;
                spring.PreSpringWidth = preSpringSize;
                spring.PostSpringWidth = postSpringSize;
                Springs[start] = spring;
            }
            else
            {
                spring = Springs[start];
                if (spring.SpringWidth < springSize)
                {
                    spring.SpringWidth = springSize;
                }
                if (spring.PreSpringWidth < preSpringSize)
                {
                    spring.PreSpringWidth = preSpringSize;
                }
                if (spring.PostSpringWidth < postSpringSize)
                {
                    spring.PostSpringWidth = postSpringSize;
                }
                if (duration < spring.SmallestDuration)
                {
                    spring.SmallestDuration = duration;
                }
                if (duration > spring.LongestDuration)
                {
                    spring.LongestDuration = duration;
                }
            }

            if (duration < SmallestDuration)
            {
                SmallestDuration = duration;
            }

            return spring;
        }

        public Spring AddBeatSpring(Beat beat, float beatSize, float preBeatSize, float postBeatSize)
        {
            return AddSpring(beat.AbsoluteStart, beat.CalculateDuration(), beatSize, preBeatSize, postBeatSize);
        }

        public void Finish()
        {
            CalculateSpringConstants();
        }

        public void CalculateSpringConstants()
        {
            var sortedSprings = _timeSortedSprings = new FastList<Spring>();
            _xMin = 0f;
            foreach (var time in Springs)
            {
                var spring = Springs[time];
                sortedSprings.Add(spring);
                if (spring.SpringWidth < _xMin)
                {
                    _xMin = spring.SpringWidth;
                }
            }

            sortedSprings.Sort((a, b) =>
            {
                if (a.TimePosition < b.TimePosition)
                {
                    return -1;
                }
                if (a.TimePosition > b.TimePosition)
                {
                    return 1;
                }
                return 0;
            });

            var totalSpringConstant = 0f;
            for (int i = 0; i < sortedSprings.Count; i++)
            {
                var currentSpring = sortedSprings[i];
                int duration;
                if (i == sortedSprings.Count - 1)
                {
                    duration = currentSpring.LongestDuration;
                }
                else
                {
                    var nextSpring = sortedSprings[i + 1];
                    duration = nextSpring.TimePosition - currentSpring.TimePosition;
                }

                currentSpring.SpringConstant = CalculateSpringConstant(currentSpring, duration);

                totalSpringConstant += 1 / currentSpring.SpringConstant;
            }

            TotalSpringConstant = 1 / totalSpringConstant;
        }

        private float CalculateSpringConstant(Spring spring, float duration)
        {
            float minDuration = spring.SmallestDuration;
            if (spring.SmallestDuration == 0)
            {
                minDuration = duration;
            }
            var phi = 1 + 0.6f * Std.Log2(duration / (float)MinDuration);
            return (minDuration / duration) * 1 / (phi * MinDurationWidth);
        }


        public float SpaceToForce(float space)
        {
            return space * TotalSpringConstant;
        }

        public float CalculateVoiceWidth(float force)
        {
            return CalculateWidth(force, TotalSpringConstant);
        }

        public float CalculateWidth(float force, float springConstant)
        {
            return force / springConstant;
        }

        public FastDictionary<int, float> BuildOnTimePositions(float force)
        {
            if (Math.Abs(_onTimePositionsForce - force) < 0.00001 && _onTimePositions != null)
            {
                return _onTimePositions;
            }

            var positions = _onTimePositions = new FastDictionary<int, float>();

            var sortedSprings = _timeSortedSprings;
            if (sortedSprings.Count == 0)
            {
                return positions;
            }

            var springX = sortedSprings[0].PreSpringWidth;
            for (int i = 0; i < sortedSprings.Count; i++)
            {
                positions[sortedSprings[i].TimePosition] = springX;
                springX += CalculateWidth(force, sortedSprings[i].SpringConstant);
            }

            return positions;
        }
    }

    public class Spring
    {
        public int TimePosition { get; set; }

        public int LongestDuration { get; set; }
        public int SmallestDuration { get; set; }

        public float Force { get; set; }
        public float Width { get; set; }
        public float SpringConstant { get; set; }

        public float SpringWidth { get; set; }
        public float PreSpringWidth { get; set; }
        public float PostSpringWidth { get; set; }
    }
}
