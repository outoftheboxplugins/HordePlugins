#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
const { spawnSync } = require("child_process");

const input = process.argv[2];
if (!input) {
  console.error("Usage: horde-plugins-cli <package>");
  process.exit(1);
}

// Allow short names: "storage-reporter" -> "@outoftheboxplugins/horde-storage-reporter"
const pkg = input.startsWith("@") ? input : `@outoftheboxplugins/horde-${input}`;

const install = spawnSync("npm", ["install", "--save", "--legacy-peer-deps", pkg], { stdio: "inherit", shell: true });
if (install.status !== 0) process.exit(install.status);

const registry = path.join(process.cwd(), "plugins", "registry.ts");
if (!fs.existsSync(registry)) {
  console.error(`plugins/registry.ts not found in ${process.cwd()}`);
  process.exit(1);
}

const line = `import "${pkg}";`;
const current = fs.readFileSync(registry, "utf8");
if (current.includes(line)) {
  console.log(`[${pkg}] already registered`);
  process.exit(0);
}

fs.appendFileSync(registry, "\n" + line + "\n");
console.log(`[${pkg}] registered in ${registry}`);
