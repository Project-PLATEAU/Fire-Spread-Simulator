import math
import numpy as np
import random
import string
import copy
import cv2

def printMethods(obj):
	for x in dir(obj):
		print( x, ':', type(eval("obj."+x)) )

def randomname(n):
	randlst = [random.choice(string.ascii_letters + string.digits) for i in range(n)]
	return ''.join(randlst)

def str2floats(x):
##  return np.array([float(i) for i in x.text.split(' ')])
## 20251208KKC gml:posListのスペース処理追加
    return np.array([float(i) for i in x.text.strip().split(' ')])

# convert (longitude[rad],latitude[rad],height[meter]) into (X,Y,Z[meter])
#
# ref: https://vldb.gsi.go.jp/sokuchi/surveycalc/surveycalc/transf.html
# ref: https://vldb.gsi.go.jp/sokuchi/surveycalc/surveycalc/algorithm/trans/trans_alg.html
# ref: http://tancro.e-central.tv/grandmaster/excel/radius.html
def convertPolarToCartsian( lat, lon, hei ):
	cosLat = math.cos( lat * math.pi/180 )
	sinLat = math.sin( lat * math.pi/180 )
	cosLon = math.cos( lon * math.pi/180 )
	sinLon = math.sin( lon * math.pi/180 )
	# Semi-mejor axis  [in meter]
	a = 6378137
	# Flattening
	f = 1 / 298.257222101
	# Eccentricity
	e = math.sqrt( 2*f - f*f )
	W = math.sqrt( 1 - e*e * sinLon*sinLon )
	# Prime vertical radius of curvature
	N = a / W
	#
	h = hei
	X = ( N + h ) * cosLat * cosLon
	Y = ( N + h ) * cosLat * sinLon
	Z = ( N * (1 - e*e) + h ) * sinLat
	return np.array([X,Y,Z])

# return left-top (latitude,longitude) and right-bottom
def convertMeshcodeToLatLon( meshcode ):
	smeshcode = str(meshcode)
	length = len(smeshcode)
	lat = int(smeshcode[0:2]) * 2 / 3
	lon = int(smeshcode[2:4]) + 100
	lat2 = lat + 2/3
	lon2 = lon + 1
	if length > 4:
		if length >= 6:
			lat += int(smeshcode[4:5]) * 2 / 3 / 8
			lon += int(smeshcode[5:6]) / 8
			lat2 = lat + 2 / 3 / 8
			lon2 = lon + 1/8
		if length >= 8:
			lat += int(smeshcode[6:7]) * 2 / 3 / 8 / 10
			lon += int(smeshcode[7:8]) / 8 / 10
			lat2 = lat + 2 / 3 / 8 / 10
			lon2 = lon + 1 / 8 / 10
	return [lat2,lon],[lat,lon2]

class VerticesTransformer:
	def __init__(self, lowerCorner=None, upperCorner=None) -> None:
		self.rot = np.eye(3)		# rotation matrix 3x3
		self.trans = np.zeros((3))	# translation vector 3
		self.scaleX = 1				# scale value of x axis
		self.aspectXY = 1			# ratio of X / Y
		if lowerCorner is not None and upperCorner is not None:
			self.calc( lowerCorner, upperCorner )

	# calculate rot, trans, scaleX, aspectXY
	#  lowerCorner, upperCorner must be [lat, lon, 0]
	def calc(self, lowerCorner, upperCorner):
		# prepare 3D points correspoinding (0,0), (0,1), (1,0)
		lt = convertPolarToCartsian(*lowerCorner)
		rt = convertPolarToCartsian( lowerCorner[0], upperCorner[1], 0 )
		lb = convertPolarToCartsian( upperCorner[0], lowerCorner[1], 0 )
		# base point
		self.trans = copy.deepcopy(lt)
		# 2 vectors
		vecx = rt - self.trans
		vecy = lb - self.trans
		# aspect ratio X/Y
		self.aspectXY = np.linalg.norm(vecx) / np.linalg.norm(vecy)
		# scale X by vecx
		self.scaleX = 1 / np.linalg.norm(vecx)
		vecx *= self.scaleX
		vecy *= self.scaleX
		# rotation on Z axis
		angleZ = math.atan2( vecx[1], vecx[0] )
		rotZ = cv2.Rodrigues( np.array([0,0,-angleZ]) )[0].T
		# rotation on Y axis
		angleY = math.atan2( vecx[2], vecx[0]/math.cos(angleZ) )
		rotY = cv2.Rodrigues( np.array([0,-angleY,0]) )[0].T
		rot = rotZ.dot( rotY )
		# apply for vecy
		vecy = vecy.dot( rot )
		# rotation on X axis
		angleX = math.atan2( vecy[2], vecy[1] )
		rotX = cv2.Rodrigues( np.array([-angleX,0,0]) )[0].T
		rot = rot.dot( rotX )
		self.rot = rot

	def transform(self, v, normscale=1, normaspect=True ):
		vv = (v - self.trans).dot(self.rot)
		if normscale is not None:
			vv *= self.scaleX * normscale
		if normaspect:
			vv[:,1] *= self.aspectXY
		return vv

	def inv_transform(self, vv, normscale=1, normaspect=True ):
		invrot = np.linalg.inv(self.rot)
		v = copy.deepcopy(vv)
		if normaspect:
			v[:,1] /= self.aspectXY
		if normscale is not None:
			v /= (self.scaleX * normscale)
		return v.dot( invrot ) + self.trans


# create Open3D box
#  translation (numpy.ndarray[float64[3, 1]]) – A 3D vector to transform the geometry
def createOpen3Dbox(size=1,translation=None, bLineSet=True, color=None):
	import open3d as o3d
	mesh = o3d.geometry.TriangleMesh.create_box(width=size,height=size,depth=size)
	if translation is not None:
		mesh.translate(translation,relative=False)
	if color is not None:
		mesh.paint_uniform_color( color )
	mesh.compute_vertex_normals()
	if bLineSet:
		mesh = o3d.geometry.LineSet.create_from_triangle_mesh(mesh)
	return mesh



# https://qiita.com/sw1227/items/e7a590994ad7dcd0e8ab
def calc_xy(phi_deg, lambda_deg, z, phi0_deg, lambda0_deg):
    """ 緯度経度を平面直角座標に変換する
    - input:
        (phi_deg, lambda_deg): 変換したい緯度・経度[度]（分・秒でなく小数であることに注意）
        (phi0_deg, lambda0_deg): 平面直角座標系原点の緯度・経度[度]（分・秒でなく小数であることに注意）
    - output:
        x: 変換後の平面直角座標[m]
        y: 変換後の平面直角座標[m]
    """
    # 緯度経度・平面直角座標系原点をラジアンに直す
    phi_rad = np.deg2rad(phi_deg)
    lambda_rad = np.deg2rad(lambda_deg)
    phi0_rad = np.deg2rad(phi0_deg)
    lambda0_rad = np.deg2rad(lambda0_deg)

    # 補助関数
    def A_array(n):
        A0 = 1 + (n**2)/4. + (n**4)/64.
        A1 = -     (3./2)*( n - (n**3)/8. - (n**5)/64. )
        A2 =     (15./16)*( n**2 - (n**4)/4. )
        A3 = -   (35./48)*( n**3 - (5./16)*(n**5) )
        A4 =   (315./512)*( n**4 )
        A5 = -(693./1280)*( n**5 )
        return np.array([A0, A1, A2, A3, A4, A5])

    def alpha_array(n):
        a0 = np.nan # dummy
        a1 = (1./2)*n - (2./3)*(n**2) + (5./16)*(n**3) + (41./180)*(n**4) - (127./288)*(n**5)
        a2 = (13./48)*(n**2) - (3./5)*(n**3) + (557./1440)*(n**4) + (281./630)*(n**5)
        a3 = (61./240)*(n**3) - (103./140)*(n**4) + (15061./26880)*(n**5)
        a4 = (49561./161280)*(n**4) - (179./168)*(n**5)
        a5 = (34729./80640)*(n**5)
        return np.array([a0, a1, a2, a3, a4, a5])

    # 定数 (a, F: 世界測地系-測地基準系1980（GRS80）楕円体)
    m0 = 0.9999
    a = 6378137.
    F = 298.257222101

    # (1) n, A_i, alpha_iの計算
    n = 1. / (2*F - 1)
    A_array = A_array(n)
    alpha_array = alpha_array(n)

    # (2), S, Aの計算
    A_ = ( (m0*a)/(1.+n) )*A_array[0] # [m]
    S_ = ( (m0*a)/(1.+n) )*( A_array[0]*phi0_rad + np.dot(A_array[1:], np.sin(2*phi0_rad*np.arange(1,6))) ) # [m]

    # (3) lambda_c, lambda_sの計算
    lambda_c = np.cos(lambda_rad - lambda0_rad)
    lambda_s = np.sin(lambda_rad - lambda0_rad)

    # (4) t, t_の計算
    t = np.sinh( np.arctanh(np.sin(phi_rad)) - ((2*np.sqrt(n)) / (1+n))*np.arctanh(((2*np.sqrt(n)) / (1+n)) * np.sin(phi_rad)) )
    t_ = np.sqrt(1 + t*t)

    # (5) xi', eta'の計算
    xi2  = np.arctan(t / lambda_c) # [rad]
    eta2 = np.arctanh(lambda_s / t_)

    # (6) x, yの計算
    x = A_ * (xi2 + np.sum(np.multiply(alpha_array[1:],
                                       np.multiply(np.sin(2*xi2*np.arange(1,6)),
                                                   np.cosh(2*eta2*np.arange(1,6)))))) - S_ # [m]
    y = A_ * (eta2 + np.sum(np.multiply(alpha_array[1:],
                                        np.multiply(np.cos(2*xi2*np.arange(1,6)),
                                                    np.sinh(2*eta2*np.arange(1,6)))))) # [m]
    # return
    return y, x, z# [m]	//	xは北が正、yは東が正