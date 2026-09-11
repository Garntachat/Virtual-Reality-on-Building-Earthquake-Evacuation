"""Validate authored runtime mesh data without requiring Unity."""
import json
import math
from pathlib import Path

root = Path(__file__).resolve().parents[1]
files = sorted((root / 'Assets/CEVR/Resources/Furniture').glob('*.json'))
assert len(files) == 22, f'Expected 22 models, found {len(files)}'
triangles = 0
for path in files:
    data = json.loads(path.read_text())
    flat = data['vertices']
    assert len(flat) % 3 == 0 and all(math.isfinite(v) for v in flat), path
    vertices = list(zip(flat[::3], flat[1::3], flat[2::3]))
    assert vertices and data['parts'], path
    for axis in range(3):
        values = [v[axis] for v in vertices]
        assert abs(max(values) - min(values) - 1) < 0.001, path
    for part in data['parts']:
        ids = part['triangles']
        assert ids and len(ids) % 3 == 0, path
        assert all(isinstance(i, int) and 0 <= i < len(vertices) for i in ids), path
        assert len(ids) < 65535, f'{path}: requires 32-bit mesh indices'
        assert len(part['color']) == 3 and all(0 <= c <= 1 for c in part['color']), path
        triangles += len(ids) // 3
    assert path.with_suffix('.json.meta').exists(), path
print(f'PASS: {len(files)} authored models, {triangles} triangles; valid indices, bounds, materials and metadata.')
