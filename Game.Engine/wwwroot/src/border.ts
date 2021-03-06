import * as PIXI from "pixi.js";

const worldDefinition = { dangerColor: 0xff0000, edgeColor: 0x0000ff, edgeWidth: 40 };

export class Border {
    container: PIXI.Container;
    graphics = new PIXI.Graphics();
    worldSize = 6000;
    constructor(container: PIXI.Container) {
        this.container = container;

        this.updateWorldSize(6000);
        this.container.addChild(this.graphics);
    }

    updateWorldSize(size: number): void {
        const edgeWidth = 4000;
        this.graphics.clear();
        this.graphics.beginFill(worldDefinition.dangerColor, 26 / 255);
        this.graphics.drawRect(-size - edgeWidth, -size - edgeWidth, 2 * size + 2 * edgeWidth, edgeWidth);
        this.graphics.drawRect(-size - edgeWidth, -size, edgeWidth, 2 * size);
        this.graphics.drawRect(+size, -size, edgeWidth, 2 * size);
        this.graphics.drawRect(-size - edgeWidth, +size, 2 * size + 2 * edgeWidth, edgeWidth);
        this.graphics.endFill();
        this.graphics.lineStyle(worldDefinition.edgeWidth, worldDefinition.edgeColor, 1);
        this.graphics.drawRect(-size, -size, size * 2, size * 2);

        this.worldSize = size;
    }
}
