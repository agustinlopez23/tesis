// Server-side gating; never expose an activation switch through NEXT_PUBLIC variables.
export function isRankingPreviewEnvironment() {
  return process.env.NODE_ENV === "development" || process.env.NODE_ENV === "test";
}

export function rankingPreviewEnabled() {
  return isRankingPreviewEnvironment() && process.env.RANKING_PREVIEW_ENABLED === "true";
}
