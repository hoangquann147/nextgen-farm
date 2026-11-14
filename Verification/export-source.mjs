import { readdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
async function collect(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const nested = await Promise.all(entries.map(async (entry) => {
    const full = path.join(directory, entry.name);
    return entry.isDirectory() ? collect(full) : full.endsWith('.cs') ? [full] : [];
  }));
  return nested.flat();
}
const files = (await collect(path.join(root, 'Assets', 'Farm'))).sort();
const parts = [
  '# Toàn bộ mã C# — NextGen Farm / Unity 6 HDRP 17.x',
  '',
  'Các khối dưới đây chứa nguyên văn mọi file C# trong Assets/Farm, không lược bỏ thân hàm. ' +
  'Tạo từng file đúng đường dẫn; giữ Editor riêng. Đọc README.vi.md để cài đặt và biết phạm vi kiểm chứng. ' +
  'Đây là source kit của bốn nhóm hệ thống; chưa có co-op thực hoặc asset photorealistic.',
  '',
];
for (const file of files) {
  const relative = path.relative(root, file).replaceAll('\\', '/');
  parts.push(`## ${relative}`, '', '```csharp', (await readFile(file, 'utf8')).trimEnd(), '```', '');
}
await writeFile(path.join(root, 'CODE_FULL.md'), parts.join('\n'), 'utf8');
console.log(`Exported ${files.length} complete C# files to CODE_FULL.md`);
