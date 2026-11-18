#!/usr/bin/env python3
"""Build the animated Workshop preview (.steamworkshop/Preview.gif) from the artist's Preview.mp4.

    python3 Scripts/preview-gif.py                      # .steamworkshop/Preview.mp4 -> .steamworkshop/Preview.gif
    python3 Scripts/preview-gif.py --work /tmp/preview  # keep the intermediates for inspection

Adapted from ToxinWarcasket's Scripts/preview-gif.py, which keeps the video's own framing where the
family's first method (OverlordWarcasket) composites the video's hero onto About/Preview.png. This
delivery (1920x1080 H.264, 12 fps padded to 29.97, 5 s) is the PNG's layout at exactly 3x, but the hero
and the lettering hold still while the background moves behind them (the rays rotate, a satellite and an
asteroid drift). PNG pixels pasted over the video would carry frame 0's background in every antialiased
edge, which would shimmer as the rays sweep past, so the GIF is the video downscaled. The stream carries
no colour matrix tag: decoded as BT.709 the hero's saturation is 113.7 against the PNG's 113.1, as
BT.601 (ffmpeg's guess) 109.5 and visibly duller.

Steps, each with its intermediate left in --work:

1. frames/      ffmpeg extracts every frame losslessly (Krita's Windows build through wslpath when no
                `ffmpeg` is on PATH). The 12 fps animation is padded to 29.97 in a 3/2 cadence, so frames
                are taken one per 1/12 s slot (indices 0, 2, 5, 7, ...): 60 frames.
2. mask.png     Pixels that ever change by more than 12/255 against frame 0 (at 640x360, box-filtered),
                opened 3 px, dilated 11 px: 53% of the frame. Its holes are the static hero and letters
                and stay holes. Feathered 2 px for the composite; the binary mask tells the encoder which
                pixels may ever change.
3. c640/        Every frame downscaled with a Hamming filter (Lanczos rings on a 3x reduction, and LZW pays
                for that ringing every frame). Outside the mask the frame is background.png, the mean of
                all 60 frames, which removes the codec's frame-to-frame noise from everything that does
                not move.
4. held/        The frames as the GIF shows them. A clean encode is 1.50 MiB, so each pixel holds one
                colour for as long as the true colour stays within BAND of it, then moves on: every frame
                shows every pixel within BAND of the truth. The runs are the longest possible (a run ends
                when the truth's spread over it would exceed 2 x BAND, and shows its mean, kept inside the
                band), which is half the repaints of holding a colour until the truth has left it behind,
                for the same worst error.
5. Preview.gif  One global 256-colour palette (libimagequant over three spread frames). Frame 0 is
                dithered outside the mask and not inside it, like every later frame, so frame 0 -> 1 shows
                no texture jump. Later frames carry only the pixels whose run ended. Outside the mask
                nothing ever changes, the badge included, so the gif-patch skill can bump it in place:
                the output reads the video's "1.6". Delays run 8/8/9 cs so 60 frames take exactly 5.00 s.

Tuning record (640x360, 60 frames, 53% mask): BAND 2 1.11 MiB, 3 0.93 MiB (shipped), 4 0.80 MiB. The
rays are flat bands whose edges move a pixel or two a frame, so the hold only has to absorb the codec's
shimmer and the palette is most of the error: 4.4 rms of BAND 3's 4.65 against the clean frames. Holding
a colour until the truth has moved a gate away from it needs a gate of 8 to fit and leaves 5.1. The
build is deterministic: a rerun on the same inputs is byte-identical.
`Docs/Research/Preview-GIF-Transcode.md` is the long record of this delivery.

Requires Pillow built with libimagequant (Pillow 9.0's binding returns a one-colour palette for odd
sizes, so the palette is learned at 640 wide and the colour count asserted), gifsicle, and ffmpeg.
Nothing here ships; the MP4 stays outside the repo like the gameplay recordings.
"""
import argparse, glob, os, shutil, subprocess, sys, tempfile
from PIL import Image, ImageChops, ImageFilter, ImageMath, ImageStat

SIZE = (640, 360)
MOTION_THRESHOLD = 12   # /255, against frame 0 at 640x360
BAND = 3                # /255: the furthest a shown pixel may sit from its true colour
DELAYS = (8, 8, 9)      # centiseconds, repeating: 60 frames = 500 cs
LIMIT = 1048576
KRITA_FFMPEG = '/mnt/c/Program Files (x86)/Steam/steamapps/common/Krita/krita/bin/ffmpeg.exe'


def log(msg):
    print(f'[preview-gif] {msg}', flush=True)


def max_channel(im):
    r, g, b = im.split()
    return ImageChops.lighter(ImageChops.lighter(r, g), b)


def max_channel_diff(a, b):
    return max_channel(ImageChops.difference(a, b))


def fresh(im):
    """Pillow keeps RGB as four bytes per pixel, and composite() and paste() blend their mask into the
    padding byte, which libimagequant then reads as alpha and builds a visibly worse palette from. A
    fresh buffer pads with 255, exactly as the frame would reload from its PNG."""
    return Image.frombytes('RGB', im.size, im.tobytes())


def extract_frames(mp4, work, ffmpeg):
    out = os.path.join(work, 'frames')
    if glob.glob(os.path.join(out, 'f*.png')):
        log(f'frames: reusing {out}')
    else:
        os.makedirs(out, exist_ok=True)
        if ffmpeg.lower().endswith('.exe'):
            win = lambda p: subprocess.check_output(['wslpath', '-w', p], text=True).strip()
            src, pattern = win(mp4), win(out) + '\\f%03d.png'
        else:
            src, pattern = mp4, os.path.join(out, 'f%03d.png')
        log(f'frames: {os.path.basename(ffmpeg)} -> {out}')
        subprocess.run([ffmpeg, '-v', 'error', '-y', '-i', src, '-an', '-fps_mode', 'passthrough',
                        '-vf', 'scale=in_color_matrix=bt709:in_range=tv:out_range=pc,format=rgb24',
                        pattern], check=True)
    paths = sorted(glob.glob(os.path.join(out, 'f*.png')))
    slots = [i * 5 // 2 for i in range(len(paths) * 2 // 5)]   # 0, 2, 5, 7, ...: one per 1/12 s
    frames = [Image.open(paths[i]).convert('RGB') for i in slots]
    # Each slot must be a fresh picture (the cadence holds) and its padding copies must not be.
    for i in slots[1:]:
        a, b = Image.open(paths[i - 1]).convert('RGB'), Image.open(paths[i]).convert('RGB')
        assert ImageStat.Stat(max_channel_diff(a.resize(SIZE, Image.BOX), b.resize(SIZE, Image.BOX))).mean[0] > 0.1 \
            or i == slots[-1], f'frame {i} is not a new picture; the 3/2 cadence assumption broke'
    log(f'frames: {len(frames)} slots of {len(paths)} frames')
    return frames


def build_mask(small, work):
    mx = Image.new('L', SIZE, 0)
    for im in small[1:]:
        mx = ImageChops.lighter(mx, max_channel_diff(im, small[0]))
    m = mx.point(lambda v: 255 if v > MOTION_THRESHOLD else 0)
    m = m.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MaxFilter(11))
    feathered = m.filter(ImageFilter.GaussianBlur(2))
    m.save(os.path.join(work, 'mask.png'))
    log(f'mask: {m.histogram()[255] / (SIZE[0] * SIZE[1]):.0%} of the frame, bbox {m.getbbox()}')
    return m, feathered


def temporal_mean(frames):
    out = []
    for ch in range(3):
        acc = None
        for f in frames:
            c = f.getchannel(ch).convert('F')
            acc = c if acc is None else ImageMath.eval('a + b', a=acc, b=c)
        out.append(ImageMath.eval('convert(a / n + 0.5, "L")', a=acc, n=float(len(frames))))
    return Image.merge('RGB', out)


def composite(uniq, feathered, work):
    out = os.path.join(work, 'c640'); os.makedirs(out, exist_ok=True)
    small = [im.resize(SIZE, Image.HAMMING) for im in uniq]
    background = temporal_mean(small)
    background.save(os.path.join(work, 'background.png'))
    frames = []
    for i, im in enumerate(small):
        c = fresh(Image.composite(im, background, feathered))
        c.save(os.path.join(out, f'c{i:02d}.png')); frames.append(c)
    return frames


def hold(comps, work):
    """Each pixel's colour over the loop as the fewest constant runs that stay within BAND of the truth."""
    out = os.path.join(work, 'held'); os.makedirs(out, exist_ok=True)
    over = lambda hi, lo: max_channel(ImageChops.subtract(hi, lo)).point(lambda v: 255 if v > 2 * BAND else 0)
    wide = lambda im: [c.convert('I') for c in im.split()]

    def colour(lo, hi, total, count):
        """A run's mean, pulled inside the range that is within BAND of every frame in the run."""
        return Image.merge('RGB', [
            ImageMath.eval('convert(min(max((t + n / 2) / n, h - b), l + b), "L")', t=t, n=count, h=h, l=l, b=BAND)
            for l, h, t in zip(wide(lo), wide(hi), total)])

    lo, hi, total, count, ended = comps[0].copy(), comps[0].copy(), wide(comps[0]), Image.new('I', SIZE, 1), [None]
    for f in comps[1:]:
        new_lo, new_hi = ImageChops.darker(lo, f), ImageChops.lighter(hi, f)
        brk = over(new_hi, new_lo)                              # pixels whose run cannot take this frame
        ended.append((brk, colour(lo, hi, total, count)))       # the colour of the run that just ended
        new_lo.paste(f, (0, 0), brk); new_hi.paste(f, (0, 0), brk)
        lo, hi = new_lo, new_hi
        total = [ImageMath.eval('t + c', t=t, c=c) for t, c in zip(total, wide(f))]
        count = ImageMath.eval('n + 1', n=count)
        for t, c in zip(total, wide(f)):
            t.paste(c, (0, 0), brk)
        count.paste(1, (0, 0), brk)
    cur, held = colour(lo, hi, total, count), [None] * len(comps)
    for i in range(len(comps) - 1, -1, -1):
        held[i] = fresh(cur)
        if i:
            cur.paste(ended[i][1], (0, 0), ended[i][0])
    worst = max(max_channel_diff(h, c).getextrema()[1] for h, c in zip(held, comps))
    assert worst <= BAND, f'a held pixel is {worst}/255 from the truth, over BAND {BAND}'
    for i, h in enumerate(held):
        h.save(os.path.join(out, f'h{i:02d}.png'))
    return held


def encode(comps, held, binary, out_path, work):
    n = len(comps); W, H = SIZE
    sample = Image.new('RGB', (W, H * 3))
    for k, i in enumerate((0, n // 3, 2 * n // 3)):
        sample.paste(comps[i], (0, H * k))
    pal = sample.quantize(256, method=Image.LIBIMAGEQUANT)
    assert len(pal.getcolors(256)) > 8, 'libimagequant returned a collapsed palette'
    remap = lambda im: im.quantize(palette=pal, dither=Image.NONE)
    outside = binary.point(lambda v: 0 if v else 255)
    tmp = os.path.join(work, 'gif'); os.makedirs(tmp, exist_ok=True)
    first = pal.crop((0, 0, W, H)); first.paste(remap(held[0]), (0, 0), binary)
    paths = []
    for i, h in enumerate(held):
        fr = first
        if i:
            fr = remap(h); fr.paste(first, (0, 0), outside)
        p = os.path.join(tmp, f'f{i:03d}.gif'); fr.save(p, interlace=False); paths.append(p)
    args = ['gifsicle', '-O3', '--no-warnings', '--loopcount=forever']
    for i, p in enumerate(paths):
        args += [f'-d{DELAYS[i % len(DELAYS)]}', p]
    subprocess.run(args + ['-o', out_path], check=True)
    return os.path.getsize(out_path)


def main():
    global BAND
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument('--mp4', default='.steamworkshop/Preview.mp4')
    ap.add_argument('--out', default='.steamworkshop/Preview.gif')
    ap.add_argument('--work', help='intermediates directory (default: a fresh temp dir, removed on success)')
    ap.add_argument('--ffmpeg', default=shutil.which('ffmpeg') or KRITA_FFMPEG)
    ap.add_argument('--band', type=int, default=BAND, help='tuning only; the shipped value is BAND')
    ap.add_argument('--force', action='store_true', help='write even over the limit (reference encodes)')
    a = ap.parse_args()
    BAND = a.band
    work = a.work or tempfile.mkdtemp(prefix='preview-gif-')
    os.makedirs(work, exist_ok=True)
    uniq = extract_frames(a.mp4, work, a.ffmpeg)
    binary, feathered = build_mask([im.resize(SIZE, Image.BOX) for im in uniq], work)
    frames = composite(uniq, feathered, work)
    held = hold(frames, work)
    size = encode(frames, held, binary, a.out + '.tmp', work)
    if size >= LIMIT and not a.force:
        os.remove(a.out + '.tmp')
        sys.exit(f'[preview-gif] {size} bytes is over the {LIMIT} byte Workshop limit; nothing written')
    os.replace(a.out + '.tmp', a.out)
    log(f'wrote {a.out}: {size} bytes ({size / LIMIT:.1%} of the limit), {len(frames)} frames, band {BAND}')
    if not a.work:
        shutil.rmtree(work)


if __name__ == '__main__':
    main()
