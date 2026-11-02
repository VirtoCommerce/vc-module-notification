// Compiles layouts/*.mjml to ../NotificationLayouts/<Name>.html.
//
// MJML needs a numeric body width to lay out columns, but the email width is a brand setting.
// Layouts are compiled with the placeholder width below, and every occurrence of it in the output
// is then replaced with the Liquid variable, so the width stays configurable at render time.
import { readdir, readFile, writeFile, mkdir } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import mjml2html from "mjml";

const PLACEHOLDER_WIDTH = "613";
const WIDTH_VARIABLE = "{{ email_width }}";
// Gmail clips messages over 102 KB; keep the layout well below that so the content has room
const MAX_LAYOUT_BYTES = 40 * 1024;

const root = path.dirname(fileURLToPath(import.meta.url));
const layoutsSrc = path.join(root, "layouts");
const layoutsOut = path.join(root, "..", "NotificationLayouts");

await mkdir(layoutsOut, { recursive: true });

let failed = false;

for (const file of (await readdir(layoutsSrc)).filter((x) => x.endsWith(".mjml")).sort()) {
  const source = await readFile(path.join(layoutsSrc, file), "utf8");
  const result = await mjml2html(source, {
    filePath: path.join(layoutsSrc, file),
    // Soft, because Liquid in attributes such as background-color="{{ header_background }}" fails type checks;
    // the remaining errors are reported below
    validationLevel: "soft",
    minify: false,
    beautify: false,
    keepComments: false,
  });

  for (const error of result.errors.filter((x) => !x.message.includes("{{"))) {
    console.error(`${file}:${error.line} ${error.message}`);
    failed = true;
  }

  const html = replaceWidths(result.html).replace(/\r?\n/g, "\r\n");
  const bytes = Buffer.byteLength(html);
  if (bytes > MAX_LAYOUT_BYTES) {
    console.error(`${file}: ${bytes} bytes, limit ${MAX_LAYOUT_BYTES}`);
    failed = true;
  }

  const outFile = path.join(layoutsOut, file.replace(/\.mjml$/, ".html"));
  await writeFile(outFile, html);
  console.log(`${file} -> ${path.relative(root, outFile)} (${bytes} bytes)`);
}

// MJML also writes widths derived from the body width, such as 581 for a section inside 16px wrapper padding.
// Those become "email_width minus the same amount". The media query breakpoint (min-width) is left alone.
function replaceWidths(html) {
  const placeholder = Number(PLACEHOLDER_WIDTH);
  const variableName = WIDTH_VARIABLE.replace(/[{} ]/g, "");

  return html
    .replaceAll(PLACEHOLDER_WIDTH, WIDTH_VARIABLE)
    .replace(/(?<!min-)(width:|width=")(\d{3})(?=px|")/g, (match, prefix, value) => {
      const delta = placeholder - Number(value);
      return delta > 0 && delta < 200 ? `${prefix}{{ ${variableName} | minus: ${delta} }}` : match;
    });
}

if (failed) {
  process.exit(1);
}
