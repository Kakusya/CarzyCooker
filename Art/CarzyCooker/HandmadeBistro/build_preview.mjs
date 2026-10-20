// Bundles the actual exported sheets; no server or npm packages are required.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';

const base = path.dirname(fileURLToPath(import.meta.url));
function csv(name) {
    const lines = fs.readFileSync(path.join(base, name), 'utf8').trim().split(/\r?\n/);
    const headers = lines.shift().split(',');
    return lines.map(line => Object.fromEntries(line.split(',').map((value, i) => [headers[i], value])));
}
function png(relative) {
    return 'data:image/png;base64,' + fs.readFileSync(path.join(base, relative)).toString('base64');
}
const rows = csv('clips.csv');
const clips = {};
for (const row of rows) {
    const clip = clips[row.clip] ??= {
        name: row.clip, image: png(row.sheet), frames: {}, fps: Number(row.fps), loop: row.loop === '1'
    };
    const frames = clip.frames[row.direction] ??= [];
    if (frames.length !== Number(row.frame)) throw new Error(`Non-contiguous frames: ${row.clip}/${row.direction}`);
    if (!fs.existsSync(path.join(base, row.frame_file))) throw new Error(`Missing frame: ${row.frame_file}`);
    frames.push({x: Number(row.x_top_left), y: Number(row.y_top_left),
        width: Number(row.width), height: Number(row.height),
        pivot: [Number(row.pivot_x_from_left), Number(row.pivot_y_from_bottom)],
        hand: [Number(row.hand_x_from_left), Number(row.hand_y_from_bottom)]});
}
const stage = Object.fromEntries(csv('stage.csv').map(row => [row.layer,
    {x: Number(row.x_from_left), y: Number(row.y_from_top), size: Number(row.sprite_size_pixels)}]));
const pack = {clips, stage, room: png('review/bistro-corner.png'), totalFrames: rows.length};
const template = fs.readFileSync(path.join(base, 'source/preview-template.html'), 'utf8');
if ((template.match(/__BISTRO_PACK__/g) ?? []).length !== 1) throw new Error('Expected one data insertion point');
const html = template.replace('__BISTRO_PACK__', JSON.stringify(pack).replaceAll('<', '\\u003c'));
fs.writeFileSync(path.join(base, 'preview.html'), html, 'utf8');
console.log(`PREVIEW BUILT clips=${Object.keys(clips).length} frames=${rows.length} bytes=${Buffer.byteLength(html)}`);
