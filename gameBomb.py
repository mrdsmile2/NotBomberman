import time
import numpy
import GameObject
import gameExplosion

from gamePacket import Packet

class GameBomb(GameObject.GameObject):

    server = None

    def __init__(self, address, x, y, z):
        super().__init__(x, y, z)
        self.address = address
        self.location = numpy.array((0.0, 0.0, 0.0))
        self.timer_dead = 3
        self.radius = 1
        self._server = GameBomb.server
        self.dead = False

    #TODO
    #Get deltaTime from self._server
    #Check collision bomb

    def tick(self, deltaTime):
        super().update()
		
        if self.dead == True:
            return

        self.timer_dead -= deltaTime
        if self.timer_dead < 0 and self.dead == False:
            print("One bomb is dead {}".format(super().id))
            self.dead = True
            timer_packet = Packet(False, self.address, "=BIf",6, super().id, self.timer_dead)
            self._server.send_all_queue.append(timer_packet)
            self._server.bombs_explosions[super().id] = gameExplosion.GameExplosion(self.x, self.y, self.z, self.radius, self.address)
        else:
            timer_packet = Packet(False, self.address, "=BIf",6, super().id, self.timer_dead)
            self._server.send_all_queue.append(timer_packet)
