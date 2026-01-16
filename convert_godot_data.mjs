/**
 * VeilBreakers Godot -> Unity Data Converter
 * Converts .tres (Godot Resource) files to JSON for Unity
 */

import fs from 'fs';
import path from 'path';

const GODOT_PROJECT = 'C:/Users/Conner/Downloads/VeilbreakersGame';
const UNITY_PROJECT = 'C:/Users/Conner/Downloads/VeilBreakers3D';

function parseTresFile(filepath) {
    const content = fs.readFileSync(filepath, 'utf-8');

    // Find the [resource] section
    const resourceMatch = content.match(/\[resource\]([\s\S]*)/);
    if (!resourceMatch) return null;

    const resourceContent = resourceMatch[1];
    const data = {};

    // Parse each line
    const lines = resourceContent.split('\n');
    for (const line of lines) {
        const trimmed = line.trim();
        if (!trimmed || trimmed.startsWith('#') || trimmed.startsWith('script')) continue;

        // Match key = value
        const match = trimmed.match(/^(\w+)\s*=\s*(.+)$/);
        if (match) {
            const key = match[1];
            const valueStr = match[2].trim();
            data[key] = parseValue(valueStr);
        }
    }

    return data;
}

function parseValue(valueStr) {
    valueStr = valueStr.trim();

    // Boolean
    if (valueStr === 'true') return true;
    if (valueStr === 'false') return false;

    // null/nil
    if (valueStr === 'null' || valueStr === 'nil') return null;

    // Color(r, g, b, a)
    const colorMatch = valueStr.match(/Color\(\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*\)/);
    if (colorMatch) {
        return {
            r: parseFloat(colorMatch[1]),
            g: parseFloat(colorMatch[2]),
            b: parseFloat(colorMatch[3]),
            a: parseFloat(colorMatch[4])
        };
    }

    // Vector2
    const vec2Match = valueStr.match(/Vector2\(\s*([\d.-]+)\s*,\s*([\d.-]+)\s*\)/);
    if (vec2Match) {
        return { x: parseFloat(vec2Match[1]), y: parseFloat(vec2Match[2]) };
    }

    // String
    if (valueStr.startsWith('"') && valueStr.endsWith('"')) {
        return valueStr.slice(1, -1);
    }

    // Array
    if (valueStr.startsWith('[')) {
        return parseArray(valueStr);
    }

    // Dictionary
    if (valueStr.startsWith('{')) {
        return parseDict(valueStr);
    }

    // Integer
    if (/^-?\d+$/.test(valueStr)) {
        return parseInt(valueStr, 10);
    }

    // Float
    if (/^-?\d+\.\d*$/.test(valueStr)) {
        return parseFloat(valueStr);
    }

    return valueStr;
}

function parseArray(valueStr) {
    try {
        const content = valueStr.slice(1, -1).trim();
        if (!content) return [];

        const items = [];
        let depth = 0;
        let current = '';
        let inString = false;

        for (let i = 0; i < content.length; i++) {
            const char = content[i];

            if (char === '"' && (i === 0 || content[i-1] !== '\\')) {
                inString = !inString;
            }

            if (!inString) {
                if (char === '[' || char === '{') depth++;
                else if (char === ']' || char === '}') depth--;
                else if (char === ',' && depth === 0) {
                    items.push(parseValue(current.trim()));
                    current = '';
                    continue;
                }
            }

            current += char;
        }

        if (current.trim()) {
            items.push(parseValue(current.trim()));
        }

        return items;
    } catch (e) {
        return [];
    }
}

function parseDict(valueStr) {
    try {
        const content = valueStr.slice(1, -1).trim();
        if (!content) return {};

        const result = {};
        let depth = 0;
        let currentKey = null;
        let currentVal = '';
        let inString = false;
        let parsingKey = true;

        for (let i = 0; i < content.length; i++) {
            const char = content[i];

            if (char === '"' && (i === 0 || content[i-1] !== '\\')) {
                inString = !inString;
            }

            if (!inString) {
                if (char === '[' || char === '{') depth++;
                else if (char === ']' || char === '}') depth--;
                else if (char === ':' && depth === 0 && parsingKey) {
                    currentKey = currentVal.trim().replace(/^"|"$/g, '');
                    currentVal = '';
                    parsingKey = false;
                    continue;
                }
                else if (char === ',' && depth === 0) {
                    if (currentKey !== null) {
                        result[currentKey] = parseValue(currentVal.trim());
                    }
                    currentKey = null;
                    currentVal = '';
                    parsingKey = true;
                    continue;
                }
            }

            currentVal += char;
        }

        if (currentKey !== null && currentVal.trim()) {
            result[currentKey] = parseValue(currentVal.trim());
        }

        return result;
    } catch (e) {
        return {};
    }
}

function convertPath(godotPath) {
    if (!godotPath) return '';
    return godotPath.replace('res://', '').replace(/\\/g, '/');
}

function getFilesRecursive(dir, ext) {
    const files = [];
    if (!fs.existsSync(dir)) return files;

    const items = fs.readdirSync(dir);
    for (const item of items) {
        const fullPath = path.join(dir, item);
        const stat = fs.statSync(fullPath);

        if (stat.isDirectory()) {
            files.push(...getFilesRecursive(fullPath, ext));
        } else if (item.endsWith(ext)) {
            files.push(fullPath);
        }
    }

    return files;
}

function convertMonsters() {
    console.log('\n[MONSTERS]');
    const monstersDir = path.join(GODOT_PROJECT, 'data', 'monsters');
    const output = [];

    const files = getFilesRecursive(monstersDir, '.tres');
    for (const file of files) {
        const data = parseTresFile(file);
        if (data) {
            if (data.sprite_path) data.sprite_path = convertPath(data.sprite_path);
            if (data.portrait_path) data.portrait_path = convertPath(data.portrait_path);
            output.push(data);
            console.log(`  Converted: ${data.display_name || path.basename(file)}`);
        }
    }

    return output;
}

function convertSkills() {
    console.log('\n[SKILLS]');
    const skillsDir = path.join(GODOT_PROJECT, 'data', 'skills');
    const output = [];

    const files = getFilesRecursive(skillsDir, '.tres');
    for (const file of files) {
        const data = parseTresFile(file);
        if (data) {
            if (file.includes('monsters')) data.is_monster_skill = true;
            if (file.includes('heroes')) data.is_hero_skill = true;
            if (data.icon_path) data.icon_path = convertPath(data.icon_path);
            output.push(data);
            console.log(`  Converted: ${data.display_name || path.basename(file)}`);
        }
    }

    return output;
}

function convertHeroes() {
    console.log('\n[HEROES]');
    const heroesDir = path.join(GODOT_PROJECT, 'data', 'heroes');
    const output = [];

    const files = getFilesRecursive(heroesDir, '.tres');
    for (const file of files) {
        const data = parseTresFile(file);
        if (data) {
            ['sprite_path', 'portrait_path', 'battle_sprite_path'].forEach(key => {
                if (data[key]) data[key] = convertPath(data[key]);
            });
            output.push(data);
            console.log(`  Converted: ${data.display_name || path.basename(file)}`);
        }
    }

    return output;
}

function convertItems() {
    console.log('\n[ITEMS]');
    const itemsDir = path.join(GODOT_PROJECT, 'data', 'items');
    const output = [];

    for (const subdir of ['consumables', 'equipment']) {
        const subpath = path.join(itemsDir, subdir);
        const files = getFilesRecursive(subpath, '.tres');

        for (const file of files) {
            const data = parseTresFile(file);
            if (data) {
                data.item_category = subdir;
                if (data.icon_path) data.icon_path = convertPath(data.icon_path);
                output.push(data);
                console.log(`  Converted: ${data.display_name || path.basename(file)}`);
            }
        }
    }

    return output;
}

function main() {
    console.log('='.repeat(60));
    console.log('VEILBREAKERS DATA CONVERTER');
    console.log('Godot .tres -> Unity JSON');
    console.log('='.repeat(60));

    // Create output directory
    const outputDir = path.join(UNITY_PROJECT, 'Assets', 'Data');
    fs.mkdirSync(outputDir, { recursive: true });

    // Convert monsters
    const monsters = convertMonsters();
    fs.writeFileSync(path.join(outputDir, 'monsters.json'), JSON.stringify(monsters, null, 2));
    console.log(`  Total: ${monsters.length} monsters\n`);

    // Convert skills
    const skills = convertSkills();
    fs.writeFileSync(path.join(outputDir, 'skills.json'), JSON.stringify(skills, null, 2));
    console.log(`  Total: ${skills.length} skills\n`);

    // Convert heroes
    const heroes = convertHeroes();
    fs.writeFileSync(path.join(outputDir, 'heroes.json'), JSON.stringify(heroes, null, 2));
    console.log(`  Total: ${heroes.length} heroes\n`);

    // Convert items
    const items = convertItems();
    fs.writeFileSync(path.join(outputDir, 'items.json'), JSON.stringify(items, null, 2));
    console.log(`  Total: ${items.length} items\n`);

    console.log('='.repeat(60));
    console.log('CONVERSION COMPLETE!');
    console.log(`Output: ${outputDir}`);
    console.log('='.repeat(60));
}

main();
