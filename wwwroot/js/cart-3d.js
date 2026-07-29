import * as THREE from "/lib/three/three.module.js";
import { GLTFLoader } from "/lib/three/addons/loaders/GLTFLoader.js";

const CART_MODEL_URL = "/models/cart/cart.glb";
const MAX_VISIBLE_PRODUCTS = 4;
const instances = [];

document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll(".cart-3d-canvas").forEach(canvas => {
        const instance = initCart3d(canvas);
        if (instance) {
            instances.push(instance);
        }
    });

    const initialQty = Number(document.getElementById("publicCartCount")?.textContent || 0);
    updateQuantity(initialQty);
});

window.addEventListener("store-cart-updated", event => {
    updateQuantity(event.detail?.totalQty ?? 0);
});

function updateQuantity(quantity) {
    instances.forEach(instance => instance.setQuantity(quantity));
}

function initCart3d(canvas) {
    if (!canvas) {
        return null;
    }

    const button = canvas.closest(".public-cart-button");
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(36, 1, 0.1, 100);
    const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
    const loader = new GLTFLoader();

    let cartModel = null;
    let productMeshes = [];
    let quantity = 0;
    let ready = false;
    let rotationTick = 0;

    camera.position.set(2.7, 1.75, 3.5);
    camera.lookAt(0, 0.35, 0);

    scene.add(new THREE.HemisphereLight(0xffffff, 0x1d3557, 2.4));

    const keyLight = new THREE.DirectionalLight(0xffffff, 2.2);
    keyLight.position.set(3, 4, 3);
    scene.add(keyLight);

    const fillLight = new THREE.DirectionalLight(0x8cc8ff, 1.2);
    fillLight.position.set(-3, 2, -2);
    scene.add(fillLight);

    loader.load(
        CART_MODEL_URL,
        gltf => {
            cartModel = gltf.scene;
            cartModel.scale.setScalar(1.14);
            scene.add(cartModel);

            productMeshes = [];
            cartModel.traverse(node => {
                if (node.isMesh) {
                    node.frustumCulled = false;
                }

                if (node.name?.startsWith("product-")) {
                    productMeshes.push(node);
                }
            });

            ready = true;
            button?.classList.add("cart-3d-ready");
            setQuantity(quantity);
            render();
        },
        undefined,
        () => {
            button?.classList.remove("cart-3d-ready");
        }
    );

    function resize() {
        const width = Math.max(canvas.clientWidth || 72, 72);
        const height = Math.max(canvas.clientHeight || 54, 54);
        renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
        renderer.setSize(width, height, false);
        camera.aspect = width / height;
        camera.updateProjectionMatrix();
    }

    function setQuantity(nextQuantity) {
        quantity = Math.max(Number(nextQuantity) || 0, 0);
        if (!ready || !cartModel) {
            return;
        }

        const visibleProducts = Math.min(quantity, MAX_VISIBLE_PRODUCTS);
        productMeshes.forEach((mesh, index) => {
            mesh.visible = index < visibleProducts;
        });

        const fullness = Math.min(quantity, 8);
        cartModel.scale.setScalar(1.14 + fullness * 0.018);
        render();
    }

    function render() {
        resize();

        if (cartModel) {
            rotationTick += 0.012;
            cartModel.rotation.y = -0.52 + Math.sin(rotationTick) * 0.08;
        }

        renderer.render(scene, camera);

        if (ready) {
            requestAnimationFrame(render);
        }
    }

    return { setQuantity };
}
