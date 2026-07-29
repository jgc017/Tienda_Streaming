import fs from "node:fs";
import path from "node:path";
import * as THREE from "three";
import { GLTFExporter } from "three/examples/jsm/exporters/GLTFExporter.js";

const outputPath = path.resolve("wwwroot/models/cart/cart.glb");

globalThis.FileReader = class {
    readAsArrayBuffer(blob) {
        blob.arrayBuffer()
            .then(buffer => {
                this.result = buffer;
                this.onloadend?.({ target: this });
            })
            .catch(error => this.onerror?.(error));
    }
};

const scene = new THREE.Scene();
const cart = new THREE.Group();
cart.name = "ShoppingCart";
scene.add(cart);

const metal = new THREE.MeshStandardMaterial({
    color: 0xbfdfff,
    metalness: 0.8,
    roughness: 0.28
});

const darkMetal = new THREE.MeshStandardMaterial({
    color: 0x1b2f45,
    metalness: 0.65,
    roughness: 0.38
});

const glassBlue = new THREE.MeshStandardMaterial({
    color: 0x4aa3ff,
    metalness: 0.15,
    roughness: 0.4,
    transparent: true,
    opacity: 0.28
});

const productMaterials = [
    new THREE.MeshStandardMaterial({ color: 0x48b4ff, metalness: 0.05, roughness: 0.5 }),
    new THREE.MeshStandardMaterial({ color: 0x55d084, metalness: 0.05, roughness: 0.5 }),
    new THREE.MeshStandardMaterial({ color: 0xffc857, metalness: 0.05, roughness: 0.5 }),
    new THREE.MeshStandardMaterial({ color: 0xd96cff, metalness: 0.05, roughness: 0.5 })
];

function addBox(name, size, position, rotation, material) {
    const mesh = new THREE.Mesh(new THREE.BoxGeometry(size[0], size[1], size[2]), material);
    mesh.name = name;
    mesh.position.set(position[0], position[1], position[2]);
    mesh.rotation.set(rotation[0], rotation[1], rotation[2]);
    cart.add(mesh);
    return mesh;
}

function addCylinder(name, radius, depth, position, rotation, material) {
    const mesh = new THREE.Mesh(new THREE.CylinderGeometry(radius, radius, depth, 24), material);
    mesh.name = name;
    mesh.position.set(position[0], position[1], position[2]);
    mesh.rotation.set(rotation[0], rotation[1], rotation[2]);
    cart.add(mesh);
    return mesh;
}

function addRod(name, length, position, rotation, radius = 0.025) {
    return addCylinder(name, radius, length, position, rotation, metal);
}

// Basket.
addBox("basket-shell", [1.36, 0.56, 0.72], [0, 0.68, 0], [0, 0, -0.08], glassBlue);
addRod("front-top-bar", 1.42, [0, 0.98, 0.39], [0, Math.PI / 2, 0]);
addRod("back-top-bar", 1.42, [0, 0.98, -0.39], [0, Math.PI / 2, 0]);
addRod("left-top-bar", 0.78, [-0.72, 0.98, 0], [Math.PI / 2, 0, 0]);
addRod("right-top-bar", 0.78, [0.72, 0.98, 0], [Math.PI / 2, 0, 0]);

for (let i = -2; i <= 2; i++) {
    addRod(`basket-vertical-${i}`, 0.52, [i * 0.26, 0.7, 0.42], [0, 0, 0], 0.018);
    addRod(`basket-back-vertical-${i}`, 0.52, [i * 0.26, 0.7, -0.42], [0, 0, 0], 0.018);
}

for (let i = -1; i <= 1; i++) {
    addRod(`basket-side-left-${i}`, 0.78, [-0.74, 0.62 + i * 0.14, 0], [Math.PI / 2, 0, 0], 0.018);
    addRod(`basket-side-right-${i}`, 0.78, [0.74, 0.62 + i * 0.14, 0], [Math.PI / 2, 0, 0], 0.018);
}

// Base and handle.
addRod("base-front", 1.1, [0, 0.32, 0.35], [0, Math.PI / 2, 0], 0.028);
addRod("base-back", 1.1, [0, 0.32, -0.35], [0, Math.PI / 2, 0], 0.028);
addRod("base-left", 0.78, [-0.55, 0.32, 0], [Math.PI / 2, 0, 0], 0.028);
addRod("base-right", 0.78, [0.55, 0.32, 0], [Math.PI / 2, 0, 0], 0.028);
addRod("handle-left", 0.9, [-0.58, 1.1, -0.68], [0.8, 0, 0], 0.026);
addRod("handle-right", 0.9, [0.58, 1.1, -0.68], [0.8, 0, 0], 0.026);
addRod("handle-grip", 1.18, [0, 1.45, -0.98], [0, Math.PI / 2, 0], 0.035);

// Wheels.
addCylinder("wheel-front-left", 0.16, 0.08, [-0.48, 0.12, 0.35], [Math.PI / 2, 0, 0], darkMetal);
addCylinder("wheel-front-right", 0.16, 0.08, [0.48, 0.12, 0.35], [Math.PI / 2, 0, 0], darkMetal);
addCylinder("wheel-back-left", 0.13, 0.08, [-0.48, 0.12, -0.32], [Math.PI / 2, 0, 0], darkMetal);
addCylinder("wheel-back-right", 0.13, 0.08, [0.48, 0.12, -0.32], [Math.PI / 2, 0, 0], darkMetal);

// Products loaded by default; the app can hide/show or clone these later.
[
    [-0.28, 0.98, 0.04, 0],
    [0.1, 1.02, -0.08, 1],
    [0.36, 0.92, 0.12, 2],
    [-0.02, 1.2, 0.12, 3]
].forEach(([x, y, z, materialIndex], index) => {
    addBox(
        `product-${index + 1}`,
        [0.26, 0.28, 0.22],
        [x, y, z],
        [0.08 * index, 0.18 * index, -0.12 * index],
        productMaterials[materialIndex]
    );
});

cart.rotation.y = -0.45;
cart.position.y = -0.45;

const exporter = new GLTFExporter();
exporter.parse(
    scene,
    result => {
        fs.mkdirSync(path.dirname(outputPath), { recursive: true });
        fs.writeFileSync(outputPath, Buffer.from(result));
        console.log(`Created ${outputPath}`);
    },
    error => {
        console.error(error);
        process.exit(1);
    },
    { binary: true }
);
