window.initLoginShader = (containerId) => {
    const container = document.getElementById(containerId);
    if (!container || container.hasChildNodes()) return;

    const scene = new THREE.Scene();
    const camera = new THREE.OrthographicCamera(-1, 1, 1, -1, 0, 1);
    const renderer = new THREE.WebGLRenderer({ antialias: true });
    
    renderer.setPixelRatio(window.devicePixelRatio);
    renderer.domElement.style.display = 'block';
    renderer.domElement.style.width = '100%';
    renderer.domElement.style.height = '100%';
    container.appendChild(renderer.domElement);

    const uniforms = {
        time: { type: "f", value: 1.0 },
        resolution: { type: "v2", value: new THREE.Vector2() }
    };

    const material = new THREE.ShaderMaterial({
        uniforms: uniforms,
        vertexShader: `void main() { gl_Position = vec4( position, 1.0 ); }`,
        fragmentShader: `
            precision highp float;
            uniform vec2 resolution;
            uniform float time;
            float random (in float x) { return fract(sin(x)*1e4); }
            float random (vec2 st) { return fract(sin(dot(st.xy, vec2(12.9898,78.233)))* 43758.5453123); }

            void main(void) {
                vec2 uv = (gl_FragCoord.xy * 2.0 - resolution.xy) / min(resolution.x, resolution.y);
                vec2 fMosaicScal = vec2(4.0, 2.0);
                vec2 vScreenSize = vec2(256.0, 256.0);
                uv.x = floor(uv.x * vScreenSize.x / fMosaicScal.x) / (vScreenSize.x / fMosaicScal.x);
                uv.y = floor(uv.y * vScreenSize.y / fMosaicScal.y) / (vScreenSize.y / fMosaicScal.y);       
                  
                float t = time * 0.06 + random(uv.x) * 0.4;
                float lineWidth = 0.0008;
                vec3 color = vec3(0.0);
                for(int j = 0; j < 3; j++){
                    for(int i = 0; i < 5; i++){
                        color[j] += lineWidth * float(i*i) / abs(fract(t - 0.01 * float(j) + float(i) * 0.01) * 1.0 - length(uv));        
                    }
                }
                gl_FragColor = vec4(color[2], color[1], color[0], 1.0);
            }
        `
    });

    scene.add(new THREE.Mesh(new THREE.PlaneGeometry(2, 2), material));

    const resize = () => {
        if (!document.body.contains(renderer.domElement)) return;
        const rect = container.getBoundingClientRect();
        renderer.setSize(rect.width, rect.height);
        uniforms.resolution.value.set(renderer.domElement.width, renderer.domElement.height);
    };
    resize();
    window.addEventListener("resize", resize);

    let frameId;
    const animate = () => {
        // BULLETPROOF KILL SWITCH
        if (!document.body.contains(renderer.domElement)) {
            cancelAnimationFrame(frameId);
            renderer.dispose();
            material.dispose();
            return;
        }
        
        frameId = requestAnimationFrame(animate);
        uniforms.time.value += 0.05;
        renderer.render(scene, camera);
    };
    animate();
};
