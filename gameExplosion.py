import GameObject


class GameExplosion(GameObject.GameObject):

    server = None

    def __init__(self, x, y, z, radius, address):
        super().__init__(x, y, z)
        self.set_collider_circle(radius, self.onCollisionEnter)
        self.time_explosion = 0.5
        self._server = GameExplosion.server
        self.is_alive = True
        self.owner_address = address

    #call tick in gameserver
    def tick(self, delta_time):
        if self.time_explosion > 0:
            self.time_explosion -= delta_time
            if self.time_explosion <= 0:
                print("EXplosione finita")
                self.is_alive = False
                self.destroy()

    def onCollisionEnter(self, collider):
        print("COLLISIONEEEEEEEEEEEEEEEEEEEEEEEEEE")
        collider.destroy(self._server.players[self.owner_address].name)