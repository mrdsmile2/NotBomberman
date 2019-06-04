import time
import numpy
import GameObject

from gamePacket import Packet

class GamePlayer(GameObject.GameObject):

    server = None

    def __init__(self, name, address, x, y, z):
        super().__init__(x, y, z)
        self.name = name
        self.address = address
        self.last_packet_timestamp = time.perf_counter()
        self.location = numpy.array((0.0, 0.0, 0.0))
        self.velocity = numpy.array((0.0, 0.0, 0.0))
        self.player_velocity_tick = 0
        self.malus = 0
        self.send_queue = []
        self.color_player = -1
        self.start_pos = [x,y,z]
        self.set_collider_rect(0.5,0.5, None) #if there are problem, set 0.4 0.4
        self._server = GamePlayer.server
        self.is_alive = True
        

    def tick(self):
        #print("{} ticked".format(self.name))
        self.malus = 0
        super().update()

    def destroy(self, name):
        super().destroy()
        len_name = len(name)
        die_packet = Packet(False, self.address, "=BI{}s".format(len_name), 7, super().id, name.encode('utf-8'))
        self._server.send_all_queue.append(die_packet)
        self.is_alive = False
		#use this when player die