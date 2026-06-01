"""
pygame_aura_demo.py — Biofield Aura visualization driven by the C# AuraSimulator.

Controls:
  1–7      Select active layer (1=Etheric … 7=Causal)
  Up/Down  Boost / reduce selected layer's input by 0.05
  R        Reset all layers to neutral state
  Q/Esc    Quit
"""
import sys
import time

import pygame

from aura_engine import AuraEngine

# ── Display ────────────────────────────────────────────────────────────────────
WIDTH, HEIGHT = 900, 900
CENTER_X, CENTER_Y = WIDTH // 2, HEIGHT // 2
FPS = 60
BG_COLOR = (8, 8, 16)

# Draw outermost (Causal=6) first so inner rings appear on top
DRAW_ORDER = [6, 5, 4, 3, 2, 1, 0]

# Pixel scaling: scale value ~1.0–2.5 → radius 88–220 px
SCALE_PX = 88


def make_ring_surface(
    radius_x: int, radius_y: int, color_rgb: tuple, alpha: int, selected: bool
) -> pygame.Surface:
    """Return an SRCALPHA surface with a glowing ellipse ring."""
    w = radius_x * 2 + 4
    h = radius_y * 2 + 4
    surf = pygame.Surface((w, h), pygame.SRCALPHA)

    # Outer glow: slightly larger, lower alpha
    glow_alpha = max(0, alpha - 80)
    if glow_alpha > 0:
        gw = int(radius_x * 1.12) * 2 + 4
        gh = int(radius_y * 1.12) * 2 + 4
        glow = pygame.Surface((gw, gh), pygame.SRCALPHA)
        pygame.draw.ellipse(glow, (*color_rgb, glow_alpha), glow.get_rect())
        surf.blit(glow, (-(gw - w) // 2, -(gh - h) // 2))

    # Main ring fill
    pygame.draw.ellipse(surf, (*color_rgb, alpha), (0, 0, w, h))

    # Hollow out the center to create a ring shape (~18 % wall thickness)
    inner_scale = 0.78
    iw = max(4, int(w * inner_scale))
    ih = max(4, int(h * inner_scale))
    ix = (w - iw) // 2
    iy = (h - ih) // 2
    pygame.draw.ellipse(surf, (0, 0, 0, 0), (ix, iy, iw, ih))

    # Selection highlight
    if selected:
        pygame.draw.ellipse(surf, (255, 255, 255, 200), (0, 0, w, h), 2)

    return surf, w, h


def run() -> None:
    pygame.init()
    screen = pygame.display.set_mode((WIDTH, HEIGHT))
    pygame.display.set_caption("Biofield Aura Matrix — C# + Pygame")
    font     = pygame.font.SysFont("monospace", 13)
    hud_font = pygame.font.SysFont("monospace", 14, bold=True)
    clock    = pygame.time.Clock()

    engine = AuraEngine()
    selected = 0
    layer_inputs = [0.74, 0.72, 0.74, 0.76, 0.70, 0.78, 0.80]

    prev = time.monotonic()

    while True:
        now = time.monotonic()
        dt  = min(now - prev, 0.1)  # cap at 100 ms so large pauses don't snap
        prev = now

        # ── Events ─────────────────────────────────────────────────────────
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                pygame.quit()
                sys.exit()

            if event.type == pygame.KEYDOWN:
                k = event.key

                if k in (pygame.K_ESCAPE, pygame.K_q):
                    pygame.quit()
                    sys.exit()

                elif k == pygame.K_r:
                    engine.reset()
                    layer_inputs = [0.74, 0.72, 0.74, 0.76, 0.70, 0.78, 0.80]
                    for i, v in enumerate(layer_inputs):
                        engine.set_layer_input(i, v)

                elif pygame.K_1 <= k <= pygame.K_7:
                    selected = k - pygame.K_1

                elif k == pygame.K_UP:
                    layer_inputs[selected] = min(1.0, layer_inputs[selected] + 0.05)
                    engine.set_layer_input(selected, layer_inputs[selected])

                elif k == pygame.K_DOWN:
                    layer_inputs[selected] = max(0.0, layer_inputs[selected] - 0.05)
                    engine.set_layer_input(selected, layer_inputs[selected])

        # ── Simulation ─────────────────────────────────────────────────────
        engine.update(dt)
        snaps = engine.get_all_snapshots()

        # ── Render ─────────────────────────────────────────────────────────
        screen.fill(BG_COLOR)

        for layer_id in DRAW_ORDER:
            s = snaps[layer_id]
            r, g, b = s["color"]
            ri = int(min(255, r * 255))
            gi = int(min(255, g * 255))
            bi = int(min(255, b * 255))
            alpha   = int(min(230, s["energy"] * 200 + 30))
            radius_x = max(10, int(s["scale"] * SCALE_PX))
            radius_y = max(8,  int(s["scale"] * SCALE_PX * 0.68))
            is_sel   = layer_id == selected

            ring_surf, sw, sh = make_ring_surface(radius_x, radius_y, (ri, gi, bi), alpha, is_sel)
            screen.blit(ring_surf, (CENTER_X - sw // 2, CENTER_Y - sh // 2))

            # Layer name label at the right edge of the ring
            label = font.render(
                f"{s['layer_name']}  {s['energy']:.2f}",
                True, (ri, gi, bi)
            )
            label_x = CENTER_X + radius_x + 8
            label_y = CENTER_Y - label.get_height() // 2 + (layer_id - 3) * 18
            screen.blit(label, (label_x, label_y))

        # ── HUD ────────────────────────────────────────────────────────────
        sel = snaps[selected]
        hud_text = (
            f"[{selected+1}] {sel['layer_name']}  "
            f"intensity={sel['intensity']:.3f}  integrity={sel['integrity']:.3f}  "
            f"energy={sel['energy']:.3f}"
        )
        hint_text = "1-7 select  |  Up/Down adjust  |  R reset  |  Q quit"

        screen.blit(hud_font.render(hud_text, True, (220, 220, 240)), (10, 10))
        screen.blit(font.render(hint_text, True, (140, 140, 160)), (10, 30))

        pygame.display.flip()
        clock.tick(FPS)


if __name__ == "__main__":
    run()
