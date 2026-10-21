# Vehicle Prefab Maker V1

Open **Bharat Ka Safar → Vehicle Prefab Maker V1** outside Play Mode.

1. Assign separate car body, rear tyre and front tyre sprites. Artwork should face right.
2. Choose **None**, **Static** or **Ragdoll** driver mode. For a driver, assign both player body and head sprites.
3. Set tyre positions/radii, body collider, centre of mass and suspension frequency/damping.
4. Set player positions and scales. In ragdoll mode, configure seat/neck anchors, joint angle limits and masses.
5. **Build Scene Preview** to inspect the assembly. Unity's collider editor and WheelJoint2D handles can refine the geometry.
6. **Generate Reusable Prefab** creates a new prefab from the recipe. **Save Edited Preview as Prefab** saves manual edits to the preview instead.

Default output: `Assets/BharatKaSafar/Prefabs/Vehicles`. A unique filename preserves existing prefabs. Save recipes to repeat a setup.

## Assembly

The prefab has an unscaled root. The moving **Body** holds Rigidbody2D, CarController, the two WheelJoint2D connections and the builder drive component. Body Sprite is a child; tyres are independent rigidbodies under the root. Static player sprites follow Body. Ragdoll sprites use separate rigidbodies and limited seat/neck hinges; their collision filter ignores vehicle parts but allows terrain contact.

Wheel positions are chassis-local coordinates. Body collider size/offset is local to Body Sprite, so Body Scale affects it. Head Collider Radius is in vehicle units. If wheel positions are edited in the scene, also move the corresponding chassis joint anchors. Ragdoll anchors should coincide in world space.

## Driving and integration

Keyboard Horizontal input and existing CarInput pedals still call CarController. Builder physics is opt-in on generated vehicles; old cars keep their original drive path. The brake pedal first drives both wheel motors toward zero while travelling forward; below Reverse Engage Speed it drives backwards. Acceleration similarly brakes a reversing car before going forward. Released pedals coast. Ground contact permits normal wheel driving; air input applies rotation torque.

The generated hierarchy includes Car, Player and CoinTrigger tags used by the existing game, plus an optional head sensor. Existing BKS crash detection still requires the confirmed overturned pose and recovery delay. Vehicle health is not added. Place one active player vehicle per gameplay scene; the existing manager/HUD detect CarController.

## Checks

**Bharat Ka Safar → Vehicle Maker → Run V1 Validation Checks** checks all three driver modes, wheel connections, ragdoll anchors, sensors and brake/reverse transitions. These checks create and destroy temporary objects without writing prefabs.

Playtest a generated vehicle on flat ground and hills: acceleration, braking to reverse, coasting, jumps, terrain contacts, driver motion, pickup collection and crash result. Compilation and structural checks do not establish a tuned suspension or stable ragdoll at every speed.
