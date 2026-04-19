"use client";

import { create } from "zustand";

export type PlaybackMode = "historical" | "current";

interface RaceViewerState {
  mode: PlaybackMode;
  isPlaying: boolean;
  speed: 0.5 | 1 | 2 | 4;
  // playback position in seconds, relative to race start (negative for pre-race)
  position: number;
  // window slicer (seconds, relative to race start)
  windowStart: number;
  windowEnd: number;
  // total race duration (s)
  duration: number;
  raceStartOffset: number; // seconds from race "first sample" to start signal

  setMode: (m: PlaybackMode) => void;
  togglePlay: () => void;
  setSpeed: (s: 0.5 | 1 | 2 | 4) => void;
  setPosition: (p: number) => void;
  setWindow: (start: number, end: number) => void;
  setDuration: (d: number) => void;
  setRaceStartOffset: (s: number) => void;
}

export const useRaceViewerStore = create<RaceViewerState>((set) => ({
  mode: "historical",
  isPlaying: false,
  speed: 1,
  position: 0,
  windowStart: 0,
  windowEnd: 0,
  duration: 0,
  raceStartOffset: 0,

  setMode: (m) => set({ mode: m }),
  togglePlay: () => set((s) => ({ isPlaying: !s.isPlaying })),
  setSpeed: (s) => set({ speed: s }),
  setPosition: (p) => set({ position: p }),
  setWindow: (start, end) => set({ windowStart: start, windowEnd: end }),
  setDuration: (d) => set({ duration: d, windowEnd: d }),
  setRaceStartOffset: (s) => set({ raceStartOffset: s }),
}));
