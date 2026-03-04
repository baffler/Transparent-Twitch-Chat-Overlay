const fs = require('fs');
const path = require('path');
const pkg = require('./package.json');
const outDir = path.resolve(__dirname, 'dist');
if (!fs.existsSync(outDir)) fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(
    path.join(outDir, 'version.json'),
    JSON.stringify({ version: pkg.version }, null, 2)
);
console.log(`Generated dist/version.json: ${pkg.version}`);
